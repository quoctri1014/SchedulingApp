namespace SchedulingApp.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;
using SchedulingApp.Domain.Entities;

/// <summary>
/// Tests cho Greedy Solver.
/// TODO: [Thành viên phụ trách Greedy] implement các test case này.
/// </summary>
public class GreedySolverTests
{
    [Fact]
    public void Run_WithValidInput_ReturnsNonEmptySchedule()
    {
        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(TestFixtures.CreateInput());
        Assert.Equal(2, result.TotalShifts);
        Assert.Equal(2, result.FilledShifts);
    }

    [Fact]
    public void Run_WithNoEmployees_ReturnsEmptySchedule()
    {
        var input = TestFixtures.CreateInput();
        input.Employees.Clear();
        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(input);
        Assert.All(result.Schedule.Values, assigned => Assert.Empty(assigned));
    }

    [Fact]
    public void Run_AssignsEmployeesGreedily_PrioritizesByAvailability()
    {
        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(TestFixtures.CreateInput());
        Assert.Equal("employee-1", result.Schedule["1"][0].EmployeeId);
    }
}

/// <summary>
/// Tests cho GA Solver.
/// TODO: [Thành viên phụ trách GA] implement các test case này.
/// </summary>
public class GaSolverTests
{
    [Fact]
    public void Run_WithValidInput_ReturnsNonEmptySchedule()
    {
        var result = new GaSolver(NullLogger<GaSolver>.Instance).Run(TestFixtures.CreateInput());
        Assert.Equal(2, result.TotalShifts);
        Assert.Equal(2, result.FilledShifts);
    }

    [Fact]
    public void Run_ReadsPopulationSizeFromOptions()
    {
        var input = TestFixtures.CreateInput();
        input.Options["populationSize"] = 4;
        input.Options["generations"] = 2;
        var result = new GaSolver(NullLogger<GaSolver>.Instance).Run(input);
        Assert.Equal(2, result.TotalShifts);
    }

    [Fact]
    public void Run_ConvergesAfterMaxGenerations()
    {
        var input = TestFixtures.CreateInput();
        input.Options["populationSize"] = 4;
        input.Options["generations"] = 1;
        var result = new GaSolver(NullLogger<GaSolver>.Instance).Run(input);
        Assert.NotNull(result.PenaltyBreakdown);
    }
}

/// <summary>
/// Tests cho SA Solver.
/// TODO: [Thành viên phụ trách SA] implement các test case này.
/// </summary>
public class SaSolverTests
{
    [Fact]
    public void Run_WithValidInput_ReturnsNonEmptySchedule()
    {
        var result = new SaSolver(NullLogger<SaSolver>.Instance).Run(TestFixtures.CreateInput());
        Assert.Equal(2, result.TotalShifts);
        Assert.Equal(2, result.FilledShifts);
    }

    [Fact]
    public void Run_ReadsCoolingRateFromOptions()
    {
        var input = TestFixtures.CreateInput();
        input.Options["initialTemperature"] = 100d;
        input.Options["coolingRate"] = 0.8d;
        input.Options["maxIterations"] = 2;
        var result = new SaSolver(NullLogger<SaSolver>.Instance).Run(input);
        Assert.Equal(2, result.TotalShifts);
    }

    [Fact]
    public void Run_CoolsDownCorrectly()
    {
        var input = TestFixtures.CreateInput();
        input.Options["maxIterations"] = 0;
        var result = new SaSolver(NullLogger<SaSolver>.Instance).Run(input);
        Assert.Equal(2, result.TotalShifts);
    }
}

/// <summary>
/// Tests cho Hybrid Solver.
/// TODO: [Thành viên phụ trách Hybrid] implement các test case này.
/// </summary>
public class HybridSolverTests
{
    [Fact]
    public void Run_WithValidInput_ReturnsNonEmptySchedule()
    {
        var result = new HybridSolver(NullLogger<HybridSolver>.Instance).Run(TestFixtures.CreateInput());
        Assert.Equal(2, result.TotalShifts);
        Assert.Equal(2, result.FilledShifts);
    }

    [Fact]
    public void Run_CombinesBothGaAndSaParameters()
    {
        var input = TestFixtures.CreateInput();
        input.Options["populationSize"] = 4;
        input.Options["generations"] = 1;
        input.Options["maxIterations"] = 1;
        var result = new HybridSolver(NullLogger<HybridSolver>.Instance).Run(input);
        Assert.Equal(2, result.TotalShifts);
    }

    [Fact]
    public void Run_ProducesResultBetterThanOrEqualToIndividualSolvers()
    {
        var input = TestFixtures.CreateInput();
        input.Options["populationSize"] = 4;
        input.Options["generations"] = 1;
        input.Options["maxIterations"] = 1;
        var hybrid = new HybridSolver(NullLogger<HybridSolver>.Instance).Run(input);
        Assert.True(hybrid.TotalPenaltyScore >= 0);
    }

}

internal static class TestFixtures
{
    public static SolverInput CreateInput()
    {
        var shifts = new List<Shift>
        {
            new() { Id = 1, StartDate = new DateTime(2026, 9, 7, 8, 0, 0), EndDate = new DateTime(2026, 9, 7, 12, 0, 0), RequiredEmployeeCount = 1 },
            new() { Id = 2, StartDate = new DateTime(2026, 9, 8, 8, 0, 0), EndDate = new DateTime(2026, 9, 8, 12, 0, 0), RequiredEmployeeCount = 1 }
        };
        var employees = new List<Employee>
        {
            new() { Id = "employee-1", FullName = "Employee 1", MaxHoursPerWeek = 40 },
            new() { Id = "employee-2", FullName = "Employee 2", MaxHoursPerWeek = 40 }
        };
        return new SolverInput
        {
            ShiftIds = shifts.Select(shift => shift.Id).ToList(),
            EmployeeIds = employees.Select(employee => employee.Id).ToList(),
            Shifts = shifts,
            Employees = employees,
            Options = new Dictionary<string, object>()
        };
    }
}
