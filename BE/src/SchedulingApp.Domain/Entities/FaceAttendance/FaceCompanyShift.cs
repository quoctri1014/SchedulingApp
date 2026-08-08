namespace SchedulingApp.Domain.Entities.FaceAttendance;

public class FaceCompanyShift
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan ShiftStart { get; set; }
    public TimeSpan ShiftEnd { get; set; }
    public TimeSpan? LunchStart { get; set; }
    public TimeSpan? LunchEnd { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public bool IsOvernight { get; set; }
    public bool? BreakIsOvernight { get; set; }
    public int MaxOvertimeBufferMinutes { get; set; }
}
