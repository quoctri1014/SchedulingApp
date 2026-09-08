using Microsoft.Extensions.Logging.Abstractions;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Tests.Isolated;

public sealed class GaSolverTests
{
    [Fact]
    public void AlgorithmName_ReturnsGa()
    {
        var solver = new GaSolver(NullLogger<GaSolver>.Instance);

        Assert.Equal("ga", solver.AlgorithmName);
    }

    [Fact]
    public void Run_NullInput_ThrowsArgumentNullException()
    {
        var solver = new GaSolver(NullLogger<GaSolver>.Instance);

        Assert.Throws<ArgumentNullException>(() => solver.Run(null!));
    }

    [Fact]
    public void Run_ValidInput_ReturnsSuccessfulScheduleResult()
    {
        var result = Run(CreateInput(CreateShift(1), CreateEmployee("employee-1")));

        Assert.NotNull(result);
        Assert.NotNull(result.Schedule);
        Assert.Equal(1, result.TotalShifts);
        Assert.Equal(1, result.FilledShifts);
        Assert.Equal(0, result.UnfilledShifts);
        Assert.NotNull(result.PenaltyBreakdown);
        Assert.Equal(result.TotalShifts, result.FilledShifts + result.UnfilledShifts);
        Assert.Single(result.Schedule["1"]);
    }

    [Fact]
    public void Run_ValidInput_AssignsCorrectNumberOfShifts()
    {
        var shifts = new[] { CreateShift(1), CreateShift(2, startHour: 12) };
        var result = Run(CreateInput(shifts, CreateEmployee("employee-1"), CreateEmployee("employee-2")));

        Assert.Equal(2, result.TotalShifts);
        Assert.Equal(2, result.FilledShifts);
        Assert.Equal(0, result.UnfilledShifts);
        Assert.All(result.Schedule.Values, assigned => Assert.Single(assigned));
    }

    [Fact]
    public void Run_WithEligibleEmployees_AssignsSuccessfully()
    {
        var result = Run(CreateInput(CreateShift(1), CreateEmployee("eligible-1")));

        Assert.Contains(result.Schedule["1"], employee => employee.EmployeeId == "eligible-1");
        Assert.Equal(1, result.FilledShifts);
    }

    [Fact]
    public void Run_WithIneligibleEmployees_DoesNotAssign()
    {
        var shift = CreateShift(1);
        var ineligible = CreateEmployee("ineligible-1", companyId: "different-company");

        var result = Run(CreateInput(shift, ineligible));

        Assert.DoesNotContain(result.Schedule["1"], employee => employee.EmployeeId == ineligible.Id);
        Assert.Equal(0, result.FilledShifts);
        Assert.True(result.PenaltyBreakdown!["soft_understaffed_shift"] > 0);
    }

    [Fact]
    public void Run_CustomOptions_AcceptsPopulationGenerationsAndSeed()
    {
        var input = CreateInput(CreateShift(1), CreateEmployee("employee-1"));
        input.Options["populationSize"] = 2;
        input.Options["generations"] = 0;
        input.Options["seed"] = 42;

        var result = Run(input);

        Assert.Equal(1, result.FilledShifts);
        Assert.Equal(1, result.TotalShifts);
    }

    [Fact]
    public void Run_DeterministicSeed_ReturnsIdenticalResults()
    {
        var firstInput = CreateInput(CreateShift(1), CreateEmployee("employee-1"), CreateEmployee("employee-2"));
        var secondInput = CreateInput(CreateShift(1), CreateEmployee("employee-1"), CreateEmployee("employee-2"));
        firstInput.Options["seed"] = 777;
        secondInput.Options["seed"] = 777;

        var first = Run(firstInput);
        var second = Run(secondInput);

        Assert.Equal(CanonicalSchedule(first), CanonicalSchedule(second));
        Assert.Equal(first.TotalShifts, second.TotalShifts);
        Assert.Equal(first.FilledShifts, second.FilledShifts);
        Assert.Equal(first.UnfilledShifts, second.UnfilledShifts);
        Assert.Equal(first.TotalPenaltyScore, second.TotalPenaltyScore);
        Assert.Equal(first.HardViolationsCount, second.HardViolationsCount);
        Assert.Equal(first.SoftViolationsCount, second.SoftViolationsCount);
        Assert.Equal(CanonicalPenalties(first), CanonicalPenalties(second));
    }

    [Fact]
    public void Run_EmptyEmployees_HandlesGracefullyWithoutCrash()
    {
        var result = Run(CreateInput(CreateShift(1)));

        Assert.Equal(1, result.TotalShifts);
        Assert.Equal(0, result.FilledShifts);
        Assert.Equal(1, result.UnfilledShifts);
        Assert.Empty(result.Schedule["1"]);
        Assert.True(result.PenaltyBreakdown!["soft_understaffed_shift"] > 0);
        Assert.Equal(result.TotalShifts, result.FilledShifts + result.UnfilledShifts);
    }

    [Fact]
    public void Run_EmptyShifts_HandlesGracefullyWithoutCrash()
    {
        var result = Run(CreateInput(Array.Empty<Shift>(), CreateEmployee("employee-1")));

        Assert.Empty(result.Schedule);
        Assert.Equal(0, result.TotalShifts);
        Assert.Equal(0, result.FilledShifts);
        Assert.Equal(0, result.UnfilledShifts);
        Assert.Equal(0, result.TotalPenaltyScore);
        Assert.Empty(result.PenaltyBreakdown!);
    }

    [Fact]
    public void Run_DuplicateEmployeeIds_RejectsInputSafely()
    {
        var input = CreateInput(CreateShift(1), CreateEmployee("duplicate"), CreateEmployee("duplicate"));

        var exception = Assert.Throws<ArgumentException>(() => Run(input));

        Assert.Contains("Duplicate Employee.Id", exception.Message);
    }

    [Fact]
    public void Run_DuplicateShiftIds_RejectsInputSafely()
    {
        var input = CreateInput(new[] { CreateShift(1), CreateShift(1, startHour: 12) }, CreateEmployee("employee-1"));

        var exception = Assert.Throws<ArgumentException>(() => Run(input));

        Assert.Contains("Duplicate Shift.Id", exception.Message);
    }

    [Fact]
    public void Run_OverlappingShifts_ReportsOverlapPenalty()
    {
        var shifts = new[] { CreateShift(1), CreateShift(2, startHour: 10) };
        var result = Run(CreateInput(shifts, CreateEmployee("employee-1")));

        Assert.True(result.PenaltyBreakdown!.TryGetValue("hard_overlapping_shifts", out var penalty));
        Assert.True(penalty > 0);
    }

    [Fact]
    public void Run_MinRestViolation_ReportsMinRestPenalty()
    {
        var shifts = new[] { CreateShift(1), CreateShift(2, startHour: 18) };
        var input = CreateInput(shifts, CreateEmployee("employee-1"));
        input.Options["minRestHours"] = 8;

        var result = Run(input);

        Assert.True(result.PenaltyBreakdown!.TryGetValue("hard_min_rest_violation", out var penalty));
        Assert.True(penalty > 0);
    }

    [Fact]
    public void Run_WeeklyOvertime_ReportsOvertimePenalty()
    {
        var shift = CreateShift(1, durationHours: 8);
        var employee = CreateEmployee("employee-1", maxHoursPerWeek: 4);

        var result = Run(CreateInput(shift, employee));

        Assert.True(result.PenaltyBreakdown!.TryGetValue("hard_weekly_overtime", out var penalty));
        Assert.True(penalty > 0);
    }

    [Fact]
    public void Run_UnderstaffedShift_ReportsUnderstaffingPenalty()
    {
        var shift = CreateShift(1, requiredEmployeeCount: 2);
        var result = Run(CreateInput(shift, CreateEmployee("employee-1")));

        Assert.True(result.PenaltyBreakdown!.TryGetValue("soft_understaffed_shift", out var penalty));
        Assert.True(penalty > 0);
        Assert.Equal(0, result.FilledShifts);
    }

    private static ScheduleResultDto Run(SolverInput input) =>
        new GaSolver(NullLogger<GaSolver>.Instance).Run(input);

    private static SolverInput CreateInput(IEnumerable<Shift> shifts, params Employee[] employees) => new()
    {
        Shifts = shifts.ToList(),
        Employees = employees.ToList(),
        Options = new Dictionary<string, object>
        {
            ["populationSize"] = 6,
            ["tournamentSize"] = 2,
            ["elitismCount"] = 1,
            ["generations"] = 2,
            ["mutationRate"] = 0.1,
            ["crossoverRate"] = 0.8,
            ["seed"] = 12345,
            ["minRestHours"] = 8.0
        }
    };

    private static SolverInput CreateInput(Shift shift, params Employee[] employees) =>
        CreateInput(new[] { shift }, employees);

    private static Shift CreateShift(
        int id,
        int startHour = 8,
        int durationHours = 4,
        int requiredEmployeeCount = 1) => new()
    {
        Id = id,
        CompanyId = "company-1",
        DepartmentId = "department-1",
        PositionId = "position-1",
        StartDate = new DateTime(2026, 1, 5).AddHours(startHour),
        EndDate = new DateTime(2026, 1, 5).AddHours(startHour + durationHours),
        RequiredEmployeeCount = requiredEmployeeCount
    };

    private static Employee CreateEmployee(
        string id,
        string companyId = "company-1",
        string departmentId = "department-1",
        string positionId = "position-1",
        int maxHoursPerWeek = 40)
    {
        return new Employee
        {
            Id = id,
            FullName = $"Employee {id}",
            MaxHoursPerWeek = maxHoursPerWeek,
            Assignments = new List<EmployeeAssignment>
            {
                new()
                {
                    EmployeeId = id,
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    PositionId = positionId
                }
            }
        };
    }

    private static string CanonicalSchedule(ScheduleResultDto result) => string.Join(
        "|",
        result.Schedule
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{string.Join(",", pair.Value.Select(employee => employee.EmployeeId).OrderBy(id => id, StringComparer.Ordinal))}"));

    private static string CanonicalPenalties(ScheduleResultDto result) => string.Join(
        "|",
        (result.PenaltyBreakdown ?? new Dictionary<string, double>())
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value:R}"));
}
