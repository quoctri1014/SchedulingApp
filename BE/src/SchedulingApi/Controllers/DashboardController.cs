using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FaceAttendanceDbContext _faceContext;

    public DashboardController(AppDbContext context, FaceAttendanceDbContext faceContext)
    {
        _context = context;
        _faceContext = faceContext;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        var totalCompanies = await _faceContext.Companies.CountAsync();
        var totalDepartments = await _faceContext.Departments.CountAsync();
        var totalEmployees = await _context.Employees.CountAsync();
        var totalShifts = await _context.Shifts.CountAsync();

        int totalFaceUsers = 0;
        try
        {
            totalFaceUsers = await _faceContext.Users.CountAsync();
        }
        catch
        {
            // Fallback if faceContext table unavailable
        }

        var runs = await _context.AlgorithmRuns.AsNoTracking().ToListAsync();
        var totalRuns = runs.Count;
        double latestAccuracy = 100.0;

        var latestRun = runs.OrderByDescending(r => r.StartedAt).FirstOrDefault();
        if (latestRun != null && latestRun.TotalShifts > 0)
        {
            latestAccuracy = Math.Round(((double)latestRun.FilledShifts / latestRun.TotalShifts) * 100, 1);
        }

        var dto = new DashboardSummaryDto
        {
            TotalCompanies = totalCompanies,
            TotalDepartments = totalDepartments,
            TotalEmployees = totalEmployees,
            TotalShifts = totalShifts,
            TotalFaceUsers = totalFaceUsers,
            TotalAlgorithmRuns = totalRuns,
            LatestAccuracyPercentage = latestAccuracy
        };

        return Ok(new ApiResponse<DashboardSummaryDto> { Success = true, Data = dto });
    }
}
