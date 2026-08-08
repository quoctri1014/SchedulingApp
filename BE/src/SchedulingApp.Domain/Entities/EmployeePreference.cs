namespace SchedulingApp.Domain.Entities;

public class EmployeePreference
{
    public int Id { get; set; }
    
    public string EmployeeId { get; set; } = string.Empty;
    
    public DayOfWeek PreferredDayOff { get; set; }
    public TimeSpan? PreferredShiftStart { get; set; }
}
