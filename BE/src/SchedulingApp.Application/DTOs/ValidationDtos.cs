using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.DTOs;

public class AssignRequest
{
    public string EmployeeId { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;

    // Context data loaded before validation.
    public Employee? Employee { get; set; }
    public Shift? Shift { get; set; }
    public List<Shift> ExistingAssignedShifts { get; set; } = new();
    public double MinimumRestHours { get; set; } = 12;
    public bool RequiresCertificate { get; set; }
    public bool AllowCrossDepartment { get; set; }
    public bool CompanyPositionIsActive { get; set; } = true;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
