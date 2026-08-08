namespace SchedulingApp.Application.DTOs;

public class ShiftDto
{
    public int Id { get; set; }
    public string? MasterShiftId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string DepartmentId { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string PositionId { get; set; } = string.Empty;
    public string? PositionName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RequiredEmployeeCount { get; set; }
}
