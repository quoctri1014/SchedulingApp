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
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly ISolverFactory _solverFactory;
    private readonly AppDbContext _context;
    private readonly IConstraintValidator _constraintValidator;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        ISolverFactory solverFactory,
        AppDbContext context,
        IConstraintValidator constraintValidator,
        ILogger<ScheduleController> logger)
    {
        _solverFactory = solverFactory;
        _context = context;
        _constraintValidator = constraintValidator;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<ScheduleResultDto>>> Run(
        [FromBody] SolverInput input,
        [FromQuery] string? algo,
        [FromQuery] string? algorithm)
    {
        var resolvedAlgorithm = algorithm ?? algo ?? "greedy";
        _logger.LogInformation("Running schedule with algorithm: {Algorithm}", resolvedAlgorithm);

        var startedAt = DateTime.Now;
        var solver = _solverFactory.Resolve(resolvedAlgorithm);

        if (input.ShiftIds.Count == 0)
        {
            input.ShiftIds = await _context.Shifts
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.StartDate)
                .Select(s => s.Id)
                .ToListAsync();
        }

        if (input.EmployeeIds.Count == 0)
        {
            input.EmployeeIds = await _context.Employees
                .Where(e => !e.IsDeleted)
                .Select(e => e.Id)
                .ToListAsync();
        }

        // Fetch real shifts and employees from DB and pass to solver
        input.Shifts = await _context.Shifts
            .Where(s => input.ShiftIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync();
        input.Employees = await _context.Employees
            .Include(e => e.Assignments)
            .Include(e => e.Leaves)
            .Include(e => e.Preferences)
            .Where(e => input.EmployeeIds.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();

        var result = solver.Run(input);

        // Record history
        var run = new AlgorithmRun
        {
            Algorithm = algorithm,
            StartedAt = startedAt,
            CompletedAt = DateTime.Now,
            ExecutionTimeMs = result.ExecutionTimeMs,
            TotalShifts = result.TotalShifts,
            FilledShifts = result.FilledShifts,
            UnfilledShifts = result.UnfilledShifts,
            HardViolationsCount = result.HardViolationsCount,
            SoftViolationsCount = result.SoftViolationsCount,
            TotalPenaltyScore = result.TotalPenaltyScore,
            PenaltyBreakdownJson = System.Text.Json.JsonSerializer.Serialize(result.PenaltyBreakdown)
        };
        _context.AlgorithmRuns.Add(run);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<ScheduleResultDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPost("run-all")]
    public async Task<ActionResult<ApiResponse<List<AlgorithmRun>>>> RunAll([FromBody] RunAllInputDto? inputDto)
    {
        _logger.LogInformation("Running all algorithms for comparison");

        var input = inputDto?.SolverInput ?? new SolverInput
        {
            FromDate = DateTime.Now,
            ToDate = DateTime.Now
        };

        if (input.ShiftIds.Count == 0)
        {
            input.ShiftIds = await _context.Shifts
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.StartDate)
                .Select(s => s.Id)
                .ToListAsync();
        }

        if (input.EmployeeIds.Count == 0)
        {
            input.EmployeeIds = await _context.Employees
                .Where(e => !e.IsDeleted)
                .Select(e => e.Id)
                .ToListAsync();
        }

        var availableAlgos = _solverFactory.GetAvailableAlgorithms().ToList();
        var createdRuns = new List<AlgorithmRun>();

        foreach (var algo in availableAlgos)
        {
            try
            {
                var solver = _solverFactory.Resolve(algo);
                var startedAt = DateTime.Now;

                // Refresh DB data
                if (input.Shifts.Count == 0 || input.Employees.Count == 0)
                {
                    input.Shifts = await _context.Shifts
                        .Where(s => input.ShiftIds.Contains(s.Id) && !s.IsDeleted)
                        .ToListAsync();
                    input.Employees = await _context.Employees
                        .Include(e => e.Assignments)
                        .Include(e => e.Leaves)
                        .Include(e => e.Preferences)
                        .Where(e => input.EmployeeIds.Contains(e.Id) && !e.IsDeleted)
                        .ToListAsync();
                }

                var result = solver.Run(input);

                var run = new AlgorithmRun
                {
                    Algorithm = algo,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.Now,
                    ExecutionTimeMs = result.ExecutionTimeMs,
                    TotalShifts = result.TotalShifts,
                    FilledShifts = result.FilledShifts,
                    UnfilledShifts = result.UnfilledShifts,
                    HardViolationsCount = result.HardViolationsCount,
                    SoftViolationsCount = result.SoftViolationsCount,
                    TotalPenaltyScore = result.TotalPenaltyScore,
                    PenaltyBreakdownJson = System.Text.Json.JsonSerializer.Serialize(result.PenaltyBreakdown)
                };
                _context.AlgorithmRuns.Add(run);
                createdRuns.Add(run);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running algorithm {Algorithm}", algo);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<List<AlgorithmRun>>
        {
            Success = true,
            Data = createdRuns
        });
    }

    [HttpPost("validate")]
    [HttpPost("validate-assignment")]
    public async Task<ActionResult<ApiResponse<ValidationResult>>> ValidateAssignment([FromBody] AssignRequest request)
    {
        if (request.Employee == null && !string.IsNullOrEmpty(request.EmployeeId))
        {
            request.Employee = await _context.Employees
                .Include(e => e.Assignments)
                .Include(e => e.Leaves)
                .Include(e => e.Preferences)
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);
        }

        if (request.Shift == null && request.ShiftId > 0)
        {
            request.Shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == request.ShiftId);
        }

        var result = _constraintValidator.Validate(request);
        return Ok(new ApiResponse<ValidationResult>
        {
            Success = result.IsValid,
            Data = result,
            Errors = result.Errors
        });
    }

    [HttpGet("compare")]
    public ActionResult<ApiResponse<object>> CompareAlgorithms()
    {
        var runs = _context.AlgorithmRuns
            .OrderByDescending(r => r.StartedAt)
            .Take(20)
            .ToList();

        var summary = runs
            .GroupBy(r => r.Algorithm)
            .Select(g => new
            {
                Algorithm = g.Key,
                AvgExecutionTimeMs = g.Average(r => r.ExecutionTimeMs),
                AvgPenaltyScore = g.Average(r => r.TotalPenaltyScore),
                AvgFilledShifts = g.Average(r => r.FilledShifts),
                RunCount = g.Count(),
                BestScore = g.Min(r => r.TotalPenaltyScore)
            });

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                Summary = summary,
                RecentRuns = runs
            }
        });
    }
}
