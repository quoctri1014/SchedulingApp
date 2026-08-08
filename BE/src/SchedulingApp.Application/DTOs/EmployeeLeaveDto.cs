namespace SchedulingApp.Application.DTOs;

public class EmployeeLeaveDto
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsApproved { get; set; }
}
