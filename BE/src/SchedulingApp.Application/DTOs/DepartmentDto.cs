namespace SchedulingApp.Application.DTOs;

public class DepartmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
}
