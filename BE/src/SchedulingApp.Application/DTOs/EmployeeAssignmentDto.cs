namespace SchedulingApp.Application.DTOs;

public class EmployeeAssignmentDto
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string DepartmentId { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string PositionId { get; set; } = string.Empty;
    public string? PositionName { get; set; }
    public DateTime? CertificateExpiryDate { get; set; }
    public bool IsPrimary { get; set; }
    public int? PriorityOrder { get; set; }
    public float EfficiencyMultiplier { get; set; }
}
