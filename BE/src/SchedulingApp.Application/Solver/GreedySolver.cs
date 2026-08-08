using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;

namespace SchedulingApp.Application.Solver;

public class GreedySolver : ISolver
{
    private readonly ILogger<GreedySolver> _logger;

    public GreedySolver(ILogger<GreedySolver> logger)
    {
        _logger = logger;
    }

    public string AlgorithmName => "greedy";

    public ScheduleResultDto Run(SolverInput input)
    {
        _logger.LogInformation("Bắt đầu chạy thuật toán Greedy thật...");
        var sw = Stopwatch.StartNew();
        
        var result = new ScheduleResultDto();
        var schedule = new Dictionary<string, List<AssignedEmployeeDto>>();
        
        // Fetch real shifts
        var targetShifts = input.Shifts
            .OrderBy(s => s.StartDate)
            .ToList();
            
        // Fetch real employees
        var employees = input.Employees;
            
        var hoursByEmp = new Dictionary<string, double>();
        foreach(var e in employees) {
            hoursByEmp[e.Id] = 0.0;
        }

        int filledShifts = 0;
        int unfilledShifts = 0;

        foreach (var shift in targetShifts)
        {
            var assigned = new List<AssignedEmployeeDto>();
            double shiftHours = (shift.EndDate - shift.StartDate).TotalHours;
            if (shiftHours < 0) shiftHours = 0;

            foreach (var emp in employees)
            {
                if (assigned.Count >= shift.RequiredEmployeeCount) break;

                // Check constraints
                var currentHours = hoursByEmp.GetValueOrDefault(emp.Id, 0.0);
                if (currentHours + shiftHours <= emp.MaxHoursPerWeek)
                {
                    assigned.Add(new AssignedEmployeeDto { EmployeeId = emp.Id, EmployeeName = emp.FullName });
                    hoursByEmp[emp.Id] = currentHours + shiftHours;
                }
            }

            schedule[shift.Id.ToString()] = assigned;

            if (assigned.Count >= shift.RequiredEmployeeCount)
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
        
        // Calculate Penalties
        var penaltyBreakdown = new Dictionary<string, double>();
        if (unfilledShifts > 0)
        {
            penaltyBreakdown["Chưa phân đủ người"] = unfilledShifts * 40.0;
        }
        
        // Mất cân bằng giờ làm (phạt nhẹ nếu chênh lệch)
        if (employees.Count > 0)
        {
            var avgHours = hoursByEmp.Values.Average();
            var variance = hoursByEmp.Values.Sum(h => Math.Abs(h - avgHours)) / employees.Count;
            if (variance > 5)
            {
                penaltyBreakdown["Mất cân bằng giờ làm"] = variance * 2.0;
            }
        }
        
        result.PenaltyBreakdown = penaltyBreakdown;
        result.TotalPenaltyScore = penaltyBreakdown.Values.Sum();
        result.SoftViolationsCount = penaltyBreakdown.Count;

        return result;
    }
}
