namespace SchedulingApp.Domain.Entities;

public class Employee
{
    // String ID from FaceAttendanceSystem.User
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int MaxHoursPerWeek { get; set; } = 40;
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties for scheduling only
    public ICollection<EmployeeAssignment> Assignments { get; set; } = new List<EmployeeAssignment>();
    public ICollection<EmployeeLeave> Leaves { get; set; } = new List<EmployeeLeave>();
    public ICollection<EmployeePreference> Preferences { get; set; } = new List<EmployeePreference>();
}
