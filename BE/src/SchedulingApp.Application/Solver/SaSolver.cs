using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

public class SaSolver : ISolver
{
    private readonly ILogger<SaSolver> _logger;
    private readonly Random _rand = new Random();

    public SaSolver(ILogger<SaSolver> logger)
    {
        _logger = logger;
    }

    public string AlgorithmName => "sa";

    public ScheduleResultDto Run(SolverInput input)
    {
        _logger.LogInformation("Bắt đầu chạy thuật toán SA...");
        var sw = Stopwatch.StartNew();

        double initTemp = GetDoubleOption(input.Options, "initialTemperature", 1000.0);
        double coolingRate = GetDoubleOption(input.Options, "coolingRate", 0.95);
        int maxIter = GetIntOption(input.Options, "maxIterations", 1000);

        var targetShifts = input.Shifts
            .OrderBy(s => s.StartDate)
            .ToList();
            
        var employees = input.Employees;
            
        var result = new ScheduleResultDto();
        if (targetShifts.Count == 0 || employees.Count == 0)
            return result;

        // Trạng thái ban đầu ngẫu nhiên
        var currentSolution = InitializeSolution(targetShifts, employees);
        double currentFitness = CalculateFitness(currentSolution, targetShifts, employees);
        
        var bestSolution = currentSolution.Select(x => x.ToList()).ToList();
        double bestFitness = currentFitness;

        double temp = initTemp;
        var history = new List<ConvergencePointDto>
        {
            new() { Iteration = 0, BestPenalty = Math.Round(bestFitness, 2), AvgPenalty = Math.Round(currentFitness, 2) }
        };

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

            if ((iter + 1) % 50 == 0 || iter == maxIter - 1)
            {
                history.Add(new ConvergencePointDto
                {
                    Iteration = iter + 1,
                    BestPenalty = Math.Round(bestFitness, 2),
                    AvgPenalty = Math.Round(currentFitness, 2)
                });
            }

            temp *= coolingRate;
        }

        var schedule = new Dictionary<string, List<AssignedEmployeeDto>>();
        int unfilledShifts = 0;
        int filledShifts = 0;
        var penaltyBreakdown = new Dictionary<string, double>();
        
        CalculateFitness(bestSolution, targetShifts, employees, penaltyBreakdown);

        for (int j = 0; j < targetShifts.Count; j++)
        {
            var shift = targetShifts[j];
            var assignedIds = bestSolution[j];
            var assignedDtos = new List<AssignedEmployeeDto>();

            foreach (var empId in assignedIds)
            {
                var emp = employees.FirstOrDefault(e => e.Id == empId);
                if (emp != null)
                {
                    assignedDtos.Add(new AssignedEmployeeDto
                    {
                        EmployeeId = emp.Id,
                        EmployeeName = emp.FullName
                    });
                }
            }

            schedule[shift.Id.ToString()] = assignedDtos;
            if (assignedDtos.Count >= shift.RequiredEmployeeCount)
                filledShifts++;
            else
                unfilledShifts++;
        }

        result.Schedule = schedule;
        result.TotalShifts = targetShifts.Count;
        result.FilledShifts = filledShifts;
        result.UnfilledShifts = unfilledShifts;
        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        
        result.PenaltyBreakdown = penaltyBreakdown;
        result.TotalPenaltyScore = penaltyBreakdown.Values.Sum();
        result.SoftViolationsCount = penaltyBreakdown.Count;
        result.ConvergenceHistory = history;

        return result;
    }

    private static double GetDoubleOption(Dictionary<string, object> options, string key, double fallback)
    {
        if (!options.TryGetValue(key, out var value))
            return fallback;

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number))
            return number;

        return double.TryParse(value?.ToString(), out var parsed) ? parsed : fallback;
    }

    private static int GetIntOption(Dictionary<string, object> options, string key, int fallback)
    {
        if (!options.TryGetValue(key, out var value))
            return fallback;

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
            return number;

        return int.TryParse(value?.ToString(), out var parsed) ? parsed : fallback;
    }

    private List<List<string>> InitializeSolution(List<Shift> shifts, List<Employee> employees)
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
        return chrom;
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
