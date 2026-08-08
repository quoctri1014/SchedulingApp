namespace SchedulingApp.Domain.Entities;

public class AlgorithmRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Algorithm { get; set; } = string.Empty; // "greedy", "ga", "sa", "hybrid"
    public string? DatasetSize { get; set; } // "small", "medium", "large"
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime CompletedAt { get; set; } = DateTime.Now;
    public long ExecutionTimeMs { get; set; }
    public int TotalShifts { get; set; }
    public int FilledShifts { get; set; }
    public int UnfilledShifts { get; set; }
    public double TotalPenaltyScore { get; set; }
    public int HardViolationsCount { get; set; }
    public int SoftViolationsCount { get; set; }
    public string? PenaltyBreakdownJson { get; set; }
    public string? RanBy { get; set; }
}
