namespace SchedulingApp.Domain.Entities.FaceAttendance;

public class FaceCompany
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public bool IsActive { get; set; } = true;
}
