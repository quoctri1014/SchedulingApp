namespace SchedulingApp.Application.DTOs;

public class SolverInputDto
{
    public List<int> CompanyIds { get; set; } = new();
    public List<int> DepartmentIds { get; set; } = new();
    public List<int> PositionIds { get; set; } = new();
    public List<int> EmployeeIds { get; set; } = new();
    public List<int> ShiftIds { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}
