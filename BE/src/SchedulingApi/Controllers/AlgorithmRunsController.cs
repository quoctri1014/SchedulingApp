using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

public class AlgorithmSummaryDto
{
    public string Algorithm { get; set; } = string.Empty;
    public int RunCount { get; set; }
    public DateTime? LastRunAt { get; set; }
    public double AvgExecutionTimeMs { get; set; }
    public double AvgPenaltyScore { get; set; }
    public double AvgFilledPercentage { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
public class AlgorithmRunsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AlgorithmRunsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AlgorithmRun>>>> GetAll([FromQuery] string? algorithm)
    {
        var query = _context.AlgorithmRuns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(algorithm))
        {
            var key = algorithm.Trim().ToLower();
            query = query.Where(r => r.Algorithm.ToLower() == key);
        }

        var list = await query.OrderByDescending(r => r.StartedAt).ToListAsync();
        return Ok(new ApiResponse<List<AlgorithmRun>> { Success = true, Data = list });
    }

    [HttpGet("latest-by-algorithm")]
    public async Task<ActionResult<ApiResponse<List<AlgorithmRun>>>> GetLatestByAlgorithm()
    {
        var runs = await _context.AlgorithmRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync();

        var latestPerAlgo = runs
            .GroupBy(r => r.Algorithm.ToLower())
            .Select(g => g.First())
            .ToList();

        return Ok(new ApiResponse<List<AlgorithmRun>> { Success = true, Data = latestPerAlgo });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<List<AlgorithmSummaryDto>>>> GetSummary()
    {
        var runs = await _context.AlgorithmRuns.AsNoTracking().ToListAsync();

        var summaries = runs
            .GroupBy(r => r.Algorithm.ToLower())
            .Select(g => new AlgorithmSummaryDto
            {
                Algorithm = g.Key,
                RunCount = g.Count(),
                LastRunAt = g.Max(r => r.StartedAt),
                AvgExecutionTimeMs = Math.Round(g.Average(r => (double)r.ExecutionTimeMs), 1),
                AvgPenaltyScore = Math.Round(g.Average(r => r.TotalPenaltyScore), 1),
                AvgFilledPercentage = Math.Round(g.Average(r => r.TotalShifts > 0 ? ((double)r.FilledShifts / r.TotalShifts) * 100 : 0), 1)
            })
            .OrderBy(s => s.Algorithm)
            .ToList();

        return Ok(new ApiResponse<List<AlgorithmSummaryDto>> { Success = true, Data = summaries });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv()
    {
        var runs = await _context.AlgorithmRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Id,Algorithm,DatasetSize,StartedAt,CompletedAt,ExecutionTimeMs,TotalShifts,FilledShifts,UnfilledShifts,TotalPenaltyScore,HardViolationsCount,SoftViolationsCount,RanBy");

        foreach (var r in runs)
        {
            csv.AppendLine($"\"{r.Id}\",\"{r.Algorithm}\",\"{r.DatasetSize}\",\"{r.StartedAt:yyyy-MM-dd HH:mm:ss}\",\"{r.CompletedAt:yyyy-MM-dd HH:mm:ss}\",{r.ExecutionTimeMs},{r.TotalShifts},{r.FilledShifts},{r.UnfilledShifts},{r.TotalPenaltyScore},{r.HardViolationsCount},{r.SoftViolationsCount},\"{r.RanBy}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"algorithm_runs_experiment_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}
