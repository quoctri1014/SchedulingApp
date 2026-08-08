namespace SchedulingApp.Domain.Entities;

public class Shift
{
    public int Id { get; set; }
    public string? MasterShiftId { get; set; }
    
    public string CompanyId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public int RequiredEmployeeCount { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;
}
