using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

public class HybridSolver : ISolver
{
    private readonly ILogger<HybridSolver> _logger;
    private readonly Random _rand = new Random();

    public HybridSolver(ILogger<HybridSolver> logger)
    {
        _logger = logger;
    }

    public string AlgorithmName => "hybrid";

    public ScheduleResultDto Run(SolverInput input)
    {
        _logger.LogInformation("Bắt đầu chạy thuật toán Hybrid (GA + SA)...");
        var sw = Stopwatch.StartNew();

        // Tham số GA
        int popSize = input.Options.TryGetValue("populationSize", out var p) ? Convert.ToInt32(p) : 30;
        int generations = input.Options.TryGetValue("generations", out var g) ? Convert.ToInt32(g) : 50;
        double mutRate = input.Options.TryGetValue("mutationRate", out var m) ? Convert.ToDouble(m) : 0.1;
        double crossRate = input.Options.TryGetValue("crossoverRate", out var c) ? Convert.ToDouble(c) : 0.8;

        // Tham số SA
        double initTemp = input.Options.TryGetValue("initialTemperature", out var t) ? Convert.ToDouble(t) : 500.0;
        double coolingRate = input.Options.TryGetValue("coolingRate", out var cr) ? Convert.ToDouble(cr) : 0.90;
        int maxIter = input.Options.TryGetValue("maxIterations", out var i) ? Convert.ToInt32(i) : 500;

        var targetShifts = input.Shifts
            .OrderBy(s => s.StartDate)
            .ToList();
            
        var employees = input.Employees;
            
        var result = new ScheduleResultDto();
        if (targetShifts.Count == 0 || employees.Count == 0)
            return result;

        // --- GIAI ĐOẠN 1: GENETIC ALGORITHM ---
        var population = InitializePopulation(popSize, targetShifts, employees);
        List<List<string>> bestGaChromosome = population[0];
        double bestGaFitness = double.MaxValue;

        for (int gen = 0; gen < generations; gen++)
        {
            var fitnessScores = population.Select(chrom => CalculateFitness(chrom, targetShifts, employees)).ToList();
            
            for (int j = 0; j < popSize; j++)
            {
                if (fitnessScores[j] < bestGaFitness)
                {
                    bestGaFitness = fitnessScores[j];
                    bestGaChromosome = population[j];
                }
            }

            var nextGen = new List<List<List<string>>> { bestGaChromosome };
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

        // --- GIAI ĐOẠN 2: SIMULATED ANNEALING ---
        var currentSolution = bestGaChromosome.Select(x => x.ToList()).ToList();
        double currentFitness = CalculateFitness(currentSolution, targetShifts, employees);
        
        var bestSolution = currentSolution.Select(x => x.ToList()).ToList();
        double bestFitness = currentFitness;
        double temp = initTemp;

        for (int iter = 0; iter < maxIter; iter++)
        {
            var newSolution = GenerateNeighbor(currentSolution, targetShifts, employees);
            double newFitness = CalculateFitness(newSolution, targetShifts, employees);

            if (newFitness < currentFitness || Math.Exp((currentFitness - newFitness) / temp) > _rand.NextDouble())
            {
                currentSolution = newSolution;
                currentFitness = newFitness;

                if (currentFitness < bestFitness)
                {
                    bestFitness = currentFitness;
                    bestSolution = currentSolution.Select(x => x.ToList()).ToList();
                }
            }
            temp *= coolingRate;
        }

        // --- TỔNG HỢP KẾT QUẢ ---
        var schedule = new Dictionary<string, List<AssignedEmployeeDto>>();
        int unfilledShifts = 0;
        int filledShifts = 0;
        var penaltyBreakdown = new Dictionary<string, double>();
        
        CalculateFitness(bestSolution, targetShifts, employees, penaltyBreakdown);

        for (int k = 0; k < targetShifts.Count; k++)
        {
            var shift = targetShifts[k];
            var assignedIds = bestSolution[k];
            
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

    private List<List<string>> TournamentSelection(List<List<List<string>>> population, List<double> fitnessScores)
    {
        int bestIdx = _rand.Next(population.Count);
        double bestFit = fitnessScores[bestIdx];

        for (int i = 1; i < 3; i++)
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
        var assigned = child[shiftIdx];

        if (assigned.Count > 0)
        {
            int empIdx = _rand.Next(assigned.Count);
            assigned[empIdx] = employees[_rand.Next(employees.Count)].Id;
        }
    }

    private List<List<string>> GenerateNeighbor(List<List<string>> currentSolution, List<Shift> shifts, List<Employee> employees)
    {
        var neighbor = currentSolution.Select(x => x.ToList()).ToList();
        int shiftIdx = _rand.Next(shifts.Count);
        var assigned = neighbor[shiftIdx];

        if (assigned.Count > 0)
        {
            int empIdx = _rand.Next(assigned.Count);
            assigned[empIdx] = employees[_rand.Next(employees.Count)].Id;
        }

        return neighbor;
    }

    private double CalculateFitness(List<List<string>> solution, List<Shift> shifts, List<Employee> employees, Dictionary<string, double>? breakdown = null)
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
            var assigned = solution[i];
            double hours = (shift.EndDate - shift.StartDate).TotalHours;
            if (hours < 0) hours = 0;

            if (assigned.Count < shift.RequiredEmployeeCount)
            {
                penaltyUnderstaffed += (shift.RequiredEmployeeCount - assigned.Count) * 40.0;
            }

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
}
