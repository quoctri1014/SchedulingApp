using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Domain.Entities.FaceAttendance;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CompanyShiftsController : ControllerBase
{
    private readonly FaceAttendanceDbContext _context;

    public CompanyShiftsController(FaceAttendanceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FaceCompanyShift>>>> GetAll()
    {
        var items = await _context.CompanyShifts.ToListAsync();
        return Ok(new ApiResponse<List<FaceCompanyShift>> { Success = true, Data = items });
    }
}
