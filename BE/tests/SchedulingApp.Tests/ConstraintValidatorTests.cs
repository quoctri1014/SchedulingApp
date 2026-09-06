namespace SchedulingApp.Tests;

using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Solver;

/// <summary>
/// Tests cho IConstraintValidator.
/// TODO: Mỗi test method dưới đây cần được implement khi có ConstraintValidator thật.
/// </summary>
public class ConstraintValidatorTests
{
    [Fact]
    public void Validate_OverlappingShifts_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.ExistingShifts.Add(new ScheduledShiftDto { EmployeeId = "employee-1", Start = request.ShiftStart.AddHours(-1), End = request.ShiftStart.AddHours(2) });
        AssertInvalid(request, "trùng giờ");
    }

    [Fact]
    public void Validate_OvernightShift_CalculatesRestCorrectly()
    {
        var request = ValidRequest();
        request.ShiftStart = new DateTime(2026, 9, 7, 14, 0, 0);
        request.ShiftEnd = new DateTime(2026, 9, 7, 22, 0, 0);
        request.ExistingShifts.Add(new ScheduledShiftDto { EmployeeId = "employee-1", Start = new DateTime(2026, 9, 6, 22, 0, 0), End = new DateTime(2026, 9, 6, 6, 0, 0) });
        var result = new ConstraintValidator().Validate(request);
        Assert.DoesNotContain(result.Errors, error => error.Contains("Thời gian nghỉ"));
    }

    [Fact]
    public void Validate_RestTimeBelowMinimum_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.ExistingShifts.Add(new ScheduledShiftDto { EmployeeId = "employee-1", Start = request.ShiftStart.AddHours(-4), End = request.ShiftStart.AddHours(-2) });
        AssertInvalid(request, "Thời gian nghỉ");
    }

    [Fact]
    public void Validate_ExpiredCertification_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.PositionRequiresCertificate = true;
        request.CertificateExpiryDate = request.ShiftStart.AddMinutes(-1);
        AssertInvalid(request, "Chứng chỉ");
    }

    [Fact]
    public void Validate_EmployeeOnLeave_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.LeavePeriods.Add(new LeavePeriodDto { Start = request.ShiftStart.AddHours(-1), End = request.ShiftEnd.AddHours(1), IsApproved = true });
        AssertInvalid(request, "nghỉ phép");
    }

    [Fact]
    public void Validate_ExceedsMaxWeeklyHours_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.HoursAlreadyScheduledThisWeek = 40;
        AssertInvalid(request, "giới hạn");
    }

    [Fact]
    public void Validate_CrossDepartmentWithSkill_ReturnsValid()
    {
        var request = ValidRequest();
        request.DepartmentId = "other-department";
        request.Assignments[0].DepartmentId = "home-department";
        request.Assignments[0].AllowCrossDepartment = true;
        Assert.True(new ConstraintValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Validate_CrossDepartmentWithoutPermission_ReturnsInvalid()
    {
        var request = ValidRequest();
        request.DepartmentId = "other-department";
        AssertInvalid(request, "assignment");
    }

    [Fact]
    public void Validate_CompanyPositionIsActive_ReturnsValid()
    {
        Assert.True(new ConstraintValidator().Validate(ValidRequest()).IsValid);
    }

    private static AssignRequest ValidRequest() => new()
    {
        EmployeeId = "employee-1",
        ShiftId = "shift-1",
        CompanyId = "company-1",
        DepartmentId = "department-1",
        PositionId = "position-1",
        ShiftStart = new DateTime(2026, 9, 7, 8, 0, 0),
        ShiftEnd = new DateTime(2026, 9, 7, 16, 0, 0),
        MaxHoursPerWeek = 40,
        Assignments = new List<EmployeeAssignmentContextDto>
        {
            new() { CompanyId = "company-1", DepartmentId = "department-1", PositionId = "position-1", CertificateExpiryDate = new DateTime(2027, 1, 1) }
        },
        CompanyPositions = new List<CompanyPositionContextDto>
        {
            new() { CompanyId = "company-1", PositionId = "position-1", IsActive = true }
        }
    };

    private static void AssertInvalid(AssignRequest request, string expectedMessagePart)
    {
        var result = new ConstraintValidator().Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase));
    }
}
