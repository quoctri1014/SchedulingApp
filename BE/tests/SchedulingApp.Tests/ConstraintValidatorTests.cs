using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Tests;

public class ConstraintValidatorTests
{
    private readonly ConstraintValidator _validator = new();

    [Fact]
    public void Validate_OverlappingShifts_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.ExistingAssignedShifts.Add(ShiftAt(2, 9, 0, 17, 0));
        AssertInvalid(request, "trùng giờ");
    }

    [Fact]
    public void Validate_OvernightShift_CalculatesRestCorrectly()
    {
        var request = ValidRequest();
        request.Shift = ShiftAt(1, 22, 0, 6, 0);
        request.ExistingAssignedShifts.Add(new Shift { Id = 2, StartDate = new DateTime(2026, 9, 2, 7, 0, 0), EndDate = new DateTime(2026, 9, 2, 15, 0, 0) });
        request.MinimumRestHours = 8;
        AssertInvalid(request, "Thời gian nghỉ");
    }

    [Fact]
    public void Validate_RestTimeBelowMinimum_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.ExistingAssignedShifts.Add(new Shift { Id = 2, StartDate = new DateTime(2026, 8, 31, 18, 0, 0), EndDate = new DateTime(2026, 9, 1, 2, 0, 0) });
        request.MinimumRestHours = 8;
        AssertInvalid(request, "Thời gian nghỉ");
    }

    [Fact]
    public void Validate_ExpiredCertification_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.RequiresCertificate = true;
        request.Employee!.Assignments.First().CertificateExpiryDate = new DateTime(2026, 8, 31);
        AssertInvalid(request, "Chứng chỉ");
    }

    [Fact]
    public void Validate_EmployeeOnLeave_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.Employee!.Leaves.Add(new EmployeeLeave { IsApproved = true, StartTime = new DateTime(2026, 9, 1), EndTime = new DateTime(2026, 9, 2) });
        AssertInvalid(request, "nghỉ phép");
    }

    [Fact]
    public void Validate_ExceedsMaxWeeklyHours_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.Employee!.MaxHoursPerWeek = 8;
        request.ExistingAssignedShifts.Add(new Shift { Id = 2, StartDate = new DateTime(2026, 9, 3, 8, 0, 0), EndDate = new DateTime(2026, 9, 3, 16, 0, 0) });
        request.MinimumRestHours = 0;
        AssertInvalid(request, "vượt giới hạn");
    }

    [Fact]
    public void Validate_CrossDepartmentWithSkill_ReturnsValid()
    {
        var request = ValidRequest();
        request.Shift!.DepartmentId = "d2";
        request.AllowCrossDepartment = true;
        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_CrossDepartmentWithoutPermission_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.Shift!.DepartmentId = "d2";
        AssertInvalid(request, "không có phân công phù hợp");
    }

    [Fact]
    public void Validate_CompanyPositionIsActive_ReturnsValid()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void Validate_InactiveCompanyPosition_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.CompanyPositionIsActive = false;
        AssertInvalid(request, "không hoạt động");
    }

    private void AssertInvalid(AssignRequest request, string expectedMessage)
    {
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));
    }

    private static AssignRequest ValidRequest()
    {
        var employee = new Employee { Id = "e1", FullName = "Vũ Trường Hùng", MaxHoursPerWeek = 40 };
        employee.Assignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id, CompanyId = "c1", DepartmentId = "d1", PositionId = "p1",
            CertificateExpiryDate = new DateTime(2027, 1, 1), EfficiencyMultiplier = 1
        });
        return new AssignRequest { Employee = employee, Shift = ShiftAt(1, 8, 0, 16, 0), MinimumRestHours = 12 };
    }

    private static Shift ShiftAt(int id, int startHour, int startMinute, int endHour, int endMinute) => new()
    {
        Id = id, CompanyId = "c1", DepartmentId = "d1", PositionId = "p1",
        StartDate = new DateTime(2026, 9, 1, startHour, startMinute, 0),
        EndDate = new DateTime(2026, 9, 1, endHour, endMinute, 0), RequiredEmployeeCount = 1
    };
}
