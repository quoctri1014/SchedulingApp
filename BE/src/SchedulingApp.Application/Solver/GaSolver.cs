using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

public class GaSolver : ISolver
{
    private readonly ILogger<GaSolver> _logger;
    private readonly Random _rand = new Random();

    public GaSolver(ILogger<GaSolver> logger)
    {
        _logger = logger;
    }

    public string AlgorithmName => "ga";

    public ScheduleResultDto Run(SolverInput input)
    {
        _logger.LogInformation("Bắt đầu chạy thuật toán GA...");
        var sw = Stopwatch.StartNew();

        int popSize = input.Options.TryGetValue("populationSize", out var p) ? Convert.ToInt32(p) : 50;
        int generations = input.Options.TryGetValue("generations", out var g) ? Convert.ToInt32(g) : 100;
        double mutRate = input.Options.TryGetValue("mutationRate", out var m) ? Convert.ToDouble(m) : 0.1;
        double crossRate = input.Options.TryGetValue("crossoverRate", out var c) ? Convert.ToDouble(c) : 0.8;

        var targetShifts = input.Shifts
            .OrderBy(s => s.StartDate)
            .ToList();
            
        var employees = input.Employees;
            
        var result = new ScheduleResultDto();
        if (targetShifts.Count == 0 || employees.Count == 0)
            return result;

        // Mã hóa: Mỗi gene là danh sách EmployeeId gán cho 1 Shift
        // Một cá thể (chromosome) là 1 lịch làm việc.
        var population = InitializePopulation(popSize, targetShifts, employees);

        List<List<string>> bestChromosome = population[0];
        double bestFitness = double.MaxValue;

        for (int gen = 0; gen < generations; gen++)
        {
            var fitnessScores = population.Select(chrom => CalculateFitness(chrom, targetShifts, employees)).ToList();
            
            // Tìm cá thể tốt nhất
            for (int i = 0; i < popSize; i++)
            {
                if (fitnessScores[i] < bestFitness)
                {
                    bestFitness = fitnessScores[i];
                    bestChromosome = population[i];
                }
            }

            var nextGen = new List<List<List<string>>>();
            
            // Tạm giữ lại cá thể tốt nhất (Elitism)
            nextGen.Add(bestChromosome);

            while (nextGen.Count < popSize)
            {
                var parent1 = TournamentSelection(population, fitnessScores);
                var parent2 = TournamentSelection(population, fitnessScores);

                var child = _rand.NextDouble() < crossRate ? Crossover(parent1, parent2) : parent1;
                
                if (_rand.NextDouble() < mutRate)
                    Mutate(child, targetShifts, employees);

                nextGen.Add(child);
            }
            population = nextGen;
        }

        // Chuyển bestChromosome thành DTO
        var schedule = new Dictionary<string, List<AssignedEmployeeDto>>();
        int unfilledShifts = 0;
        int filledShifts = 0;
        var penaltyBreakdown = new Dictionary<string, double>();
        
        var finalFitness = CalculateFitness(bestChromosome, targetShifts, employees, penaltyBreakdown);

        for (int i = 0; i < targetShifts.Count; i++)
        {
            var shift = targetShifts[i];
            var assignedIds = bestChromosome[i];
            
            var assignedDtos = new List<AssignedEmployeeDto>();
            foreach(var eid in assignedIds) {
                var emp = employees.FirstOrDefault(e => e.Id == eid);
                if (emp != null)
                {
                    assignedDtos.Add(new AssignedEmployeeDto { EmployeeId = emp.Id, EmployeeName = emp.FullName });
                }
            }

            schedule[shift.Id.ToString()] = assignedDtos;
            if (assignedDtos.Count >= shift.RequiredEmployeeCount)
                filledShifts++;
            else
                unfilledShifts++;
        }

        sw.Stop();

        result.Schedule = schedule;
        result.TotalShifts = targetShifts.Count;
        result.FilledShifts = filledShifts;
        result.UnfilledShifts = unfilledShifts;
        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        
        result.PenaltyBreakdown = penaltyBreakdown;
        result.TotalPenaltyScore = penaltyBreakdown.Values.Sum();
        result.SoftViolationsCount = penaltyBreakdown.Count;

        return result;
    }

    private List<List<List<string>>> InitializePopulation(int size, List<Shift> shifts, List<Employee> employees)
    {
        var pop = new List<List<List<string>>>();
        for (int i = 0; i < size; i++)
        {
            var chrom = new List<List<string>>();
            foreach (var s in shifts)
            {
                var assigned = new List<string>();
                int req = s.RequiredEmployeeCount;
                var shuffledEmps = employees.OrderBy(x => _rand.Next()).ToList();
                for (int j = 0; j < Math.Min(req, shuffledEmps.Count); j++)
                {
                    assigned.Add(shuffledEmps[j].Id);
                }
                chrom.Add(assigned);
            }
            pop.Add(chrom);
        }
        return pop;
    }

    private double CalculateFitness(List<List<string>> chromosome, List<Shift> shifts, List<Employee> employees, Dictionary<string, double>? breakdown = null)
    {
        double fitness = 0;
        var hoursByEmp = new Dictionary<string, double>();
        foreach (var e in employees) hoursByEmp[e.Id] = 0;

        double penaltyUnderstaffed = 0;
        double penaltyOvertime = 0;
        double penaltyOverlap = 0;

        for (int i = 0; i < shifts.Count; i++)
        {
            var shift = shifts[i];
            var assigned = chromosome[i];
            double hours = (shift.EndDate - shift.StartDate).TotalHours;
            if (hours < 0) hours = 0;

            if (assigned.Count < shift.RequiredEmployeeCount)
            {
                penaltyUnderstaffed += (shift.RequiredEmployeeCount - assigned.Count) * 40.0;
            }

            // Kiểm tra trùng lặp nhân viên trong cùng 1 ca
            var unique = assigned.Distinct().Count();
            if (unique < assigned.Count)
            {
                penaltyOverlap += (assigned.Count - unique) * 30.0;
            }

            foreach (var empId in assigned)
            {
                if (hoursByEmp.ContainsKey(empId))
                    hoursByEmp[empId] += hours;
            }
        }

        foreach (var emp in employees)
        {
            if (hoursByEmp[emp.Id] > emp.MaxHoursPerWeek)
            {
                penaltyOvertime += (hoursByEmp[emp.Id] - emp.MaxHoursPerWeek) * 10.0;
            }
        }

        fitness = penaltyUnderstaffed + penaltyOvertime + penaltyOverlap;

        if (breakdown != null)
        {
            if (penaltyUnderstaffed > 0) breakdown["Chưa phân đủ người"] = penaltyUnderstaffed;
            if (penaltyOvertime > 0) breakdown["Làm quá giờ (Overtime)"] = penaltyOvertime;
            if (penaltyOverlap > 0) breakdown["Trùng lặp nhân viên"] = penaltyOverlap;
        }

        return fitness;
    }

    private List<List<string>> TournamentSelection(List<List<List<string>>> population, List<double> fitnessScores)
    {
        int bestIdx = _rand.Next(population.Count);
        double bestFit = fitnessScores[bestIdx];

        for (int i = 1; i < 3; i++) // Tournament size 3
        {
            int idx = _rand.Next(population.Count);
            if (fitnessScores[idx] < bestFit)
            {
                bestFit = fitnessScores[idx];
                bestIdx = idx;
            }
        }
        return population[bestIdx].Select(x => x.ToList()).ToList();
    }

    private List<List<string>> Crossover(List<List<string>> parent1, List<List<string>> parent2)
    {
        var child = new List<List<string>>();
        int splitPoint = _rand.Next(parent1.Count);

        for (int i = 0; i < parent1.Count; i++)
        {
            if (i < splitPoint) child.Add(parent1[i].ToList());
            else child.Add(parent2[i].ToList());
        }
        return child;
    }

    private void Mutate(List<List<string>> child, List<Shift> shifts, List<Employee> employees)
    {
        int shiftIdx = _rand.Next(child.Count);
        var shift = shifts[shiftIdx];
        var assigned = child[shiftIdx];

        if (assigned.Count > 0)
        {
            int empIdx = _rand.Next(assigned.Count);
            assigned[empIdx] = employees[_rand.Next(employees.Count)].Id;
        }
    }
}
