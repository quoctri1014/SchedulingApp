using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<HealthCheckDto>>> CheckHealth()
    {
        bool dbConnected = false;
        try
        {
            dbConnected = await _context.Database.CanConnectAsync();
        }
        catch
        {
            dbConnected = false;
        }

        var dto = new HealthCheckDto
        {
            Status = dbConnected ? "Healthy" : "Unhealthy",
            DatabaseConnected = dbConnected,
            Timestamp = DateTime.Now,
            Version = "1.0.0"
        };

        if (!dbConnected)
        {
            return StatusCode(503, new ApiResponse<HealthCheckDto>
            {
                Success = false,
                Data = dto,
                Errors = new List<string> { "Không thể kết nối đến cơ sở dữ liệu." }
            });
        }

        return Ok(new ApiResponse<HealthCheckDto> { Success = true, Data = dto });
    }
}
