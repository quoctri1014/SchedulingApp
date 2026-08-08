using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly ISolverFactory _solverFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(ISolverFactory solverFactory, AppDbContext context, ILogger<ScheduleController> logger)
    {
        _solverFactory = solverFactory;
        _context = context;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<ScheduleResultDto>>> Run(
        [FromBody] SolverInput input,
        [FromQuery] string algorithm = "greedy")
    {
        _logger.LogInformation("▶ Bắt đầu xếp lịch | Algorithm={Algorithm} | Shifts={ShiftCount} | Employees={EmpCount}",
            algorithm, input.ShiftIds.Count, input.EmployeeIds.Count);

        var startedAt = DateTime.Now;
        var solver = _solverFactory.Resolve(algorithm);

        // Fetch real shifts and employees from DB and pass to solver
        input.Shifts = await _context.Shifts
            .Where(s => input.ShiftIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync();
        input.Employees = await _context.Employees
            .Where(e => input.EmployeeIds.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();

        var result = solver.Run(input);
        var completedAt = DateTime.Now;

        var algoKey = algorithm.Trim().ToLower();

        // Sử dụng trực tiếp PenaltyBreakdown từ thuật toán
        if (result.PenaltyBreakdown == null)
        {
            result.PenaltyBreakdown = new Dictionary<string, double>();
        }

        // Tự động lưu vết lịch sử AlgorithmRun vào Database
        var run = new AlgorithmRun
        {
            Id = Guid.NewGuid(),
            Algorithm = algoKey,
            DatasetSize = input.ShiftIds.Count > 100 ? "large" : (input.ShiftIds.Count > 20 ? "medium" : "small"),
            StartedAt = startedAt,
            CompletedAt = completedAt,
            ExecutionTimeMs = result.ExecutionTimeMs > 0 ? result.ExecutionTimeMs : (long)(completedAt - startedAt).TotalMilliseconds,
            TotalShifts = result.TotalShifts,
            FilledShifts = result.FilledShifts,
            UnfilledShifts = result.UnfilledShifts,
            TotalPenaltyScore = result.TotalPenaltyScore,
            HardViolationsCount = result.HardViolationsCount,
            SoftViolationsCount = result.SoftViolationsCount,
            PenaltyBreakdownJson = JsonSerializer.Serialize(result.PenaltyBreakdown),
            RanBy = "Admin"
        };

        _context.AlgorithmRuns.Add(run);
        await _context.SaveChangesAsync();

        result.AlgorithmRunId = run.Id;

        _logger.LogInformation(
            "✅ Hoàn thành xếp lịch & lưu AlgorithmRun {RunId} | Algorithm={Algorithm} | Penalty={Penalty} | Time={Ms}ms",
            run.Id, algorithm, result.TotalPenaltyScore, result.ExecutionTimeMs);

        return Ok(new ApiResponse<ScheduleResultDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPost("run-all")]
    public async Task<ActionResult<ApiResponse<List<AlgorithmRun>>>> RunAll([FromBody] RunAllInputDto? inputDto)
    {
        var runsPerAlgo = Math.Max(1, inputDto?.RunsPerAlgorithm ?? 1);
        var datasetSize = string.IsNullOrWhiteSpace(inputDto?.DatasetSize) ? "medium" : inputDto.DatasetSize;

        var input = inputDto?.SolverInput ?? new SolverInput
        {
            ShiftIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            EmployeeIds = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" },
            FromDate = DateTime.Now,
            ToDate = DateTime.Now
        };

        var availableAlgos = _solverFactory.GetAvailableAlgorithms().ToList();
        var createdRuns = new List<AlgorithmRun>();

        _logger.LogInformation("▶ Bắt đầu Batch Run-All | Solvers={SolverCount} | RunsPerAlgo={RunsPerAlgo}",
            availableAlgos.Count, runsPerAlgo);

        foreach (var algoKey in availableAlgos)
        {
            for (int i = 0; i < runsPerAlgo; i++)
            {
                var startedAt = DateTime.Now;
                var solver = _solverFactory.Resolve(algoKey);

                // Fetch real shifts and employees from DB and pass to solver
                if (input.Shifts == null || input.Shifts.Count == 0)
                {
                    input.Shifts = await _context.Shifts
                        .Where(s => input.ShiftIds.Contains(s.Id) && !s.IsDeleted)
                        .ToListAsync();
                    input.Employees = await _context.Employees
                        .Where(e => input.EmployeeIds.Contains(e.Id) && !e.IsDeleted)
                        .ToListAsync();
                }

                var result = solver.Run(input);
                var completedAt = DateTime.Now;

                if (result.PenaltyBreakdown == null)
                {
                    result.PenaltyBreakdown = new Dictionary<string, double>();
                }

                var run = new AlgorithmRun
                {
                    Id = Guid.NewGuid(),
                    Algorithm = algoKey.ToLower(),
                    DatasetSize = datasetSize,
                    StartedAt = startedAt,
                    CompletedAt = completedAt,
                    ExecutionTimeMs = result.ExecutionTimeMs > 0 ? result.ExecutionTimeMs : (long)(completedAt - startedAt).TotalMilliseconds,
                    TotalShifts = result.TotalShifts,
                    FilledShifts = result.FilledShifts,
                    UnfilledShifts = result.UnfilledShifts,
                    TotalPenaltyScore = result.TotalPenaltyScore,
                    HardViolationsCount = result.HardViolationsCount,
                    SoftViolationsCount = result.SoftViolationsCount,
                    PenaltyBreakdownJson = JsonSerializer.Serialize(result.PenaltyBreakdown),
                    RanBy = "BatchRunAll"
                };

                _context.AlgorithmRuns.Add(run);
                createdRuns.Add(run);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<List<AlgorithmRun>>
        {
            Success = true,
            Data = createdRuns
        });
    }

    [HttpGet("compare")]
    public ActionResult<ApiResponse<object>> CompareAlgorithms()
    {
        return StatusCode(501, new ApiResponse<object>
        {
            Success = false,
            Data = null,
            Errors = new List<string> { "Chức năng so sánh thuật toán không được hỗ trợ qua API trực tiếp. Số liệu so sánh được tổng hợp từ dữ liệu thực tế tại Dashboard." }
        });
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<AlgorithmRun>>> GetLatest()
    {
        var latest = await _context.AlgorithmRuns
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();

        return Ok(new ApiResponse<AlgorithmRun> { Success = true, Data = latest });
    }
}
