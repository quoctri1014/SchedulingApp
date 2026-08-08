namespace SchedulingApp.Domain.Entities.FaceAttendance;

public class FaceJobTitle
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
