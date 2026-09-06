namespace SchedulingApp.Application.DTOs;

public class AssignRequest
{
    public string EmployeeId { get; set; } = string.Empty;
    public string ShiftId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }
    public int MaxHoursPerWeek { get; set; } = 40;
    public double HoursAlreadyScheduledThisWeek { get; set; }
    public double MinRestHours { get; set; } = 8;
    public bool PositionRequiresCertificate { get; set; }
    public DateTime? CertificateExpiryDate { get; set; }
    public List<ScheduledShiftDto> ExistingShifts { get; set; } = new();
    public List<LeavePeriodDto> LeavePeriods { get; set; } = new();
    public List<EmployeeAssignmentContextDto> Assignments { get; set; } = new();
    public List<CompanyPositionContextDto> CompanyPositions { get; set; } = new();
}

public class ScheduledShiftDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class LeavePeriodDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsApproved { get; set; }
}

public class EmployeeAssignmentContextDto
{
    public string CompanyId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    public bool AllowCrossDepartment { get; set; }
    public DateTime? CertificateExpiryDate { get; set; }
}

public class CompanyPositionContextDto
{
    public string CompanyId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
