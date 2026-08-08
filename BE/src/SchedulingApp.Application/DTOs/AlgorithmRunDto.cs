namespace SchedulingApp.Application.DTOs;

public class AlgorithmRunDto
{
    public Guid Id { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public string DatasetSize { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int TotalShifts { get; set; }
    public int FilledShifts { get; set; }
    public int UnfilledShifts { get; set; }
    public double TotalPenaltyScore { get; set; }
    public int HardViolationsCount { get; set; }
    public int SoftViolationsCount { get; set; }
    public string? PenaltyBreakdownJson { get; set; }
    public string RanBy { get; set; } = string.Empty;
}
