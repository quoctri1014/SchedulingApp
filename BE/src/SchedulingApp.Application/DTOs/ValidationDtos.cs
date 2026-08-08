namespace SchedulingApp.Application.DTOs;

public class AssignRequest
{
    public int EmployeeId { get; set; }
    public int ShiftId { get; set; }
    public int CompanyId { get; set; }
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
