using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PositionsController : ControllerBase
{
    private readonly FaceAttendanceDbContext _context;

    public PositionsController(FaceAttendanceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PositionDto>>>> GetAll()
    {
        var items = await _context.JobTitles
            .Where(j => j.IsActive)
            .OrderBy(j => j.Name)
            .Select(j => new PositionDto
            {
                Id = j.Id.ToString(),
                Name = j.Name,
                RequiresCertificate = false
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<PositionDto>> { Success = true, Data = items });
    }
}
