using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly FaceAttendanceDbContext _context;

    public CompaniesController(FaceAttendanceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CompanyDto>>>> GetAll()
    {
        var items = await _context.Companies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Description = c.ShortName
            }).ToListAsync();

        return Ok(new ApiResponse<List<CompanyDto>> { Success = true, Data = items });
    }
}
