namespace SchedulingApp.Domain.Entities;

public class EmployeeLeave
{
    public int Id { get; set; }
    
    public string EmployeeId { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public bool IsApproved { get; set; } = false;
}
