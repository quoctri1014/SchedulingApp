namespace SchedulingApp.Application.DTOs;

public class EmployeeDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int MaxHoursPerWeek { get; set; }
    
    public List<EmployeeAssignmentDto> Assignments { get; set; } = new();
    public List<EmployeeLeaveDto> Leaves { get; set; } = new();
    public List<EmployeePreferenceDto> Preferences { get; set; } = new();
}
