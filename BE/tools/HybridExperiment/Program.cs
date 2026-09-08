using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;
using SchedulingApp.Domain.Entities;

var outputPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath("hybrid_experiment_results.json");
var runs = new List<ExperimentRun>();
var configurations = new[]
{
    new DatasetConfig("Small", 10, 18, 1),
    new DatasetConfig("Medium", 30, 42, 2),
    new DatasetConfig("Large", 100, 84, 3)
};

foreach (var config in configurations)
{
    foreach (var localIterations in new[] { 0, 100 })
    {
        for (var run = 1; run <= 30; run++)
        {
            var input = BuildInput(config, run, localIterations);
            var result = new HybridSolver(NullLogger<HybridSolver>.Instance).Run(input);
            runs.Add(new ExperimentRun(
                config.Name, localIterations > 0 ? "Hybrid có Local Search" : "Hybrid không Local Search",
                run, config.EmployeeCount, config.ShiftCount, localIterations,
                result.ExecutionTimeMs, result.TotalPenaltyScore, result.HardViolationsCount,
                result.SoftViolationsCount, result.FilledShifts, result.UnfilledShifts));
        }
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(runs, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Đã ghi {runs.Count} lần chạy vào {outputPath}");

static SolverInput BuildInput(DatasetConfig config, int seed, int localIterations)
{
    var employees = new List<Employee>();
    for (var index = 0; index < config.EmployeeCount; index++)
    {
        var department = $"d{index % 3 + 1}";
        var position = $"p{index % 2 + 1}";
        var employee = new Employee
        {
            Id = $"e{index + 1}", FullName = $"Nhân viên {index + 1}", MaxHoursPerWeek = 40
        };
        employee.Assignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id, CompanyId = "c1", DepartmentId = department,
            PositionId = position, CertificateExpiryDate = new DateTime(2027, 12, 31),
            EfficiencyMultiplier = 1, PriorityOrder = index + 1
        });
        if (index % 7 == 0)
            employee.Preferences.Add(new EmployeePreference { EmployeeId = employee.Id, PreferredDayOff = DayOfWeek.Sunday });
        employees.Add(employee);
    }

    var shifts = new List<Shift>();
    var startDate = new DateTime(2026, 9, 7);
    for (var index = 0; index < config.ShiftCount; index++)
    {
        var day = startDate.AddDays(index / 3);
        var slot = index % 3;
        var startHour = slot switch { 0 => 6, 1 => 14, _ => 22 };
        var start = day.AddHours(startHour);
        var end = start.AddHours(8);
        shifts.Add(new Shift
        {
            Id = index + 1, CompanyId = "c1", DepartmentId = $"d{index % 3 + 1}",
            PositionId = $"p{index % 2 + 1}", StartDate = start, EndDate = end,
            RequiredEmployeeCount = config.RequiredPerShift
        });
    }

    // A small, reproducible disruption forces the optimizer to handle real constraints.
    if (employees.Count > 5)
        employees[0].Leaves.Add(new EmployeeLeave { EmployeeId = employees[0].Id, IsApproved = true, StartTime = startDate, EndTime = startDate.AddDays(1) });

    return new SolverInput
    {
        Employees = employees, Shifts = shifts, ShiftIds = shifts.Select(s => s.Id).ToList(),
        EmployeeIds = employees.Select(e => e.Id).ToList(), FromDate = shifts.Min(s => s.StartDate), ToDate = shifts.Max(s => s.EndDate),
        Options = new Dictionary<string, object>
        {
            ["populationSize"] = 12, ["generations"] = 15, ["mutationRate"] = 0.15,
            ["crossoverRate"] = 0.8, ["localSearchIterations"] = localIterations,
            ["minimumRestHours"] = 8, ["randomSeed"] = seed
        }
    };
}

record DatasetConfig(string Name, int EmployeeCount, int ShiftCount, int RequiredPerShift);
record ExperimentRun(string Dataset, string Variant, int Run, int EmployeeCount, int ShiftCount,
    int LocalSearchIterations, long ExecutionTimeMs, double TotalPenaltyScore, int HardViolationsCount,
    int SoftViolationsCount, int FilledShifts, int UnfilledShifts);
