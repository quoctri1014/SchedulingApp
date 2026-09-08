using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.DTOs;

public class SolverInput
{
    public List<string> CompanyIds { get; set; } = new();
    public List<string> DepartmentIds { get; set; } = new();
    public List<string> PositionIds { get; set; } = new();
    public List<string> EmployeeIds { get; set; } = new();
    public List<int> ShiftIds { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
    public List<Shift> Shifts { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
}

public class RunAllInputDto
{
    public int RunsPerAlgorithm { get; set; } = 1;
    public string? DatasetSize { get; set; } = "medium";
    public SolverInput? SolverInput { get; set; }
}

public class AssignedEmployeeDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class ConvergencePointDto
{
    public int Iteration { get; set; }
    public double BestPenalty { get; set; }
    public double AvgPenalty { get; set; }
}

public class ScheduleResultDto
{
    public Dictionary<string, List<AssignedEmployeeDto>> Schedule { get; set; } = new();
    public int TotalShifts { get; set; }
    public int FilledShifts { get; set; }
    public int UnfilledShifts { get; set; }
    public long ExecutionTimeMs { get; set; }
    public double TotalPenaltyScore { get; set; }
    public int HardViolationsCount { get; set; }
    public int SoftViolationsCount { get; set; }
    public Dictionary<string, double>? PenaltyBreakdown { get; set; } = new();
    public Guid? AlgorithmRunId { get; set; }
    public List<ConvergencePointDto> ConvergenceHistory { get; set; } = new();
    public string? EarlyStoppingReason { get; set; }
    public int? CompletedGenerations { get; set; }
}
