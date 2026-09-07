using Microsoft.Extensions.Logging.Abstractions;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Tests;

/// <summary>
/// Tests cho Greedy Solver.
/// TODO: [Thành viên phụ trách Greedy] implement các test case này.
/// </summary>
public class GreedySolverTests
{
    [Fact(Skip = "TODO: cài đặt logic thật cho GreedySolver")]
    public void Run_WithValidInput_ReturnsNonEmptySchedule() { }

    [Fact(Skip = "TODO: cài đặt logic thật cho GreedySolver")]
    public void Run_WithNoEmployees_ReturnsEmptySchedule() { }

    [Fact(Skip = "TODO: cài đặt logic thật cho GreedySolver")]
    public void Run_AssignsEmployeesGreedily_PrioritizesByAvailability() { }
}

/// <summary>
/// Tests cho GA Solver.
/// TODO: [Thành viên phụ trách GA] implement các test case này.
/// </summary>
public class GaSolverTests
{
    [Fact(Skip = "TODO: cài đặt logic thật cho GaSolver")]
    public void Run_WithValidInput_ReturnsNonEmptySchedule() { }

    [Fact(Skip = "TODO: cài đặt logic thật cho GaSolver")]
    public void Run_ReadsPopulationSizeFromOptions() { }

    [Fact(Skip = "TODO: cài đặt logic thật cho GaSolver")]
    public void Run_ConvergesAfterMaxGenerations() { }
}

/// <summary>
/// Tests cho SA Solver.
/// TODO: [Thành viên phụ trách SA] implement các test case này.
/// </summary>
public class SaSolverTests
{
    [Fact(Skip = "TODO: cài đặt logic thật cho SaSolver")]
    public void Run_WithValidInput_ReturnsNonEmptySchedule() { }

    [Fact(Skip = "TODO: cài đặt logic thật cho SaSolver")]
    public void Run_ReadsCoolingRateFromOptions() { }

    [Fact(Skip = "TODO: cài đặt logic thật cho SaSolver")]
    public void Run_CoolsDownCorrectly() { }
}

/// <summary>
/// Tests cho Hybrid Solver.
/// TODO: [Thành viên phụ trách Hybrid] implement các test case này.
/// </summary>
public class HybridSolverTests
{
    [Fact]
    public void Run_WithValidInput_ReturnsCompleteSchedule()
    {
        var result = CreateSolver().Run(CreateInput());

        Assert.Equal(2, result.TotalShifts);
        Assert.Equal(2, result.FilledShifts);
        Assert.Equal(0, result.UnfilledShifts);
        Assert.Equal(0, result.HardViolationsCount);
        Assert.True(result.ExecutionTimeMs > 0);
    }

    [Fact]
    public void Run_RespectsMaximumWeeklyHours()
    {
        var input = CreateInput();
        input.Employees = new List<Employee>
        {
            new() { Id = "e1", FullName = "Nhân viên 1", MaxHoursPerWeek = 8 }
        };

        var result = CreateSolver().Run(input);

        Assert.Equal(1, result.FilledShifts);
        Assert.Equal(1, result.UnfilledShifts);
        Assert.True(result.HardViolationsCount >= 1);
    }

    [Fact]
    public void Run_ReadsHybridParametersAndKeepsPenaltyBreakdown()
    {
        var input = CreateInput();
        input.Options["populationSize"] = 8;
        input.Options["generations"] = 4;
        input.Options["localSearchIterations"] = 20;

        var result = CreateSolver().Run(input);

        Assert.NotNull(result.PenaltyBreakdown);
        Assert.True(result.TotalPenaltyScore >= 0);
        Assert.Equal("hybrid", CreateSolver().AlgorithmName);
    }

    private static HybridSolver CreateSolver() => new(NullLogger<HybridSolver>.Instance);

    private static SolverInput CreateInput() => new()
    {
        Shifts = new List<Shift>
        {
            new() { Id = 1, CompanyId = "c1", DepartmentId = "d1", PositionId = "p1", StartDate = new DateTime(2026, 9, 1, 8, 0, 0), EndDate = new DateTime(2026, 9, 1, 16, 0, 0), RequiredEmployeeCount = 1 },
            new() { Id = 2, CompanyId = "c1", DepartmentId = "d1", PositionId = "p1", StartDate = new DateTime(2026, 9, 2, 8, 0, 0), EndDate = new DateTime(2026, 9, 2, 16, 0, 0), RequiredEmployeeCount = 1 }
        },
        Employees = new List<Employee>
        {
            new() { Id = "e1", FullName = "Nhân viên 1", MaxHoursPerWeek = 40 },
            new() { Id = "e2", FullName = "Nhân viên 2", MaxHoursPerWeek = 40 }
        },
        Options = new Dictionary<string, object>
        {
            ["populationSize"] = 8,
            ["generations"] = 10,
            ["localSearchIterations"] = 50
        }
    };
}
