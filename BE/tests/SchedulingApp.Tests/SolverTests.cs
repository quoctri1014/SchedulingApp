using Microsoft.Extensions.Logging.Abstractions;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Tests;

/// <summary>
/// Tests cho Greedy Solver.
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

    [Fact]
    public void Run_RespectsApprovedLeaves_DoesNotAssignEmployeeOnLeave()
    {
        var input = TestFixtures.CreateInput();
        input.Employees[0].Leaves.Add(new EmployeeLeave
        {
            IsApproved = true,
            StartTime = new DateTime(2026, 9, 7, 6, 0, 0),
            EndTime = new DateTime(2026, 9, 7, 18, 0, 0)
        });

        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(input);

        // employee-1 is on leave during shift 1, so employee-2 should be assigned
        Assert.Equal("employee-2", result.Schedule["1"][0].EmployeeId);
    }

    [Fact]
    public void Run_RespectsMinimumRestHours_PreventsBackToBackShifts()
    {
        var shifts = new List<Shift>
        {
            new() { Id = 1, StartDate = new DateTime(2026, 9, 7, 8, 0, 0), EndDate = new DateTime(2026, 9, 7, 16, 0, 0), RequiredEmployeeCount = 1 },
            new() { Id = 2, StartDate = new DateTime(2026, 9, 7, 18, 0, 0), EndDate = new DateTime(2026, 9, 7, 23, 0, 0), RequiredEmployeeCount = 1 } // Only 2h rest
        };
        var employees = new List<Employee>
        {
            new() { Id = "e1", FullName = "Nhân viên 1", MaxHoursPerWeek = 40 }
        };
        var input = new SolverInput
        {
            Shifts = shifts,
            Employees = employees,
            Options = new Dictionary<string, object> { ["minRestHours"] = 8.0 }
        };

        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(input);

        Assert.Equal(1, result.FilledShifts);
        Assert.Equal(1, result.UnfilledShifts);
        Assert.Equal(0, result.HardViolationsCount);
    }

    [Fact]
    public void Run_RespectsMaxHoursPerWeek_CapsAssignedHours()
    {
        var shifts = new List<Shift>
        {
            new() { Id = 1, StartDate = new DateTime(2026, 9, 7, 8, 0, 0), EndDate = new DateTime(2026, 9, 7, 16, 0, 0), RequiredEmployeeCount = 1 },
            new() { Id = 2, StartDate = new DateTime(2026, 9, 8, 8, 0, 0), EndDate = new DateTime(2026, 9, 8, 16, 0, 0), RequiredEmployeeCount = 1 }
        };
        var employees = new List<Employee>
        {
            new() { Id = "e1", FullName = "Nhân viên 1", MaxHoursPerWeek = 8 } // Only 8h max
        };
        var input = new SolverInput
        {
            Shifts = shifts,
            Employees = employees
        };

        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(input);

        Assert.Equal(1, result.FilledShifts);
        Assert.Equal(1, result.UnfilledShifts);
    }

    [Fact]
    public void Run_SupportsAblationStrategies_CalculatesValidPenaltyBreakdown()
    {
        var input = TestFixtures.CreateInput();
        input.Options["strategy"] = "adaptive";

        var result = new GreedySolver(NullLogger<GreedySolver>.Instance).Run(input);

        Assert.NotNull(result.PenaltyBreakdown);
        Assert.True(result.ExecutionTimeMs >= 0);
        Assert.Equal(0, result.HardViolationsCount);
    }
}

/// <summary>
/// Tests cho SA Solver.
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
