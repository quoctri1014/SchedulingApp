using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly FaceAttendanceDbContext _context;

    public DepartmentsController(FaceAttendanceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetAll([FromQuery] string? companyId = null)
    {
        var query = _context.Departments.Where(d => d.IsActive).AsQueryable();
        
        if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out Guid compGuid))
        {
            query = query.Where(d => d.CompanyId == compGuid);
        }

        var items = await (from d in query
                           join c in _context.Companies on d.CompanyId equals c.Id into g
                           from c in g.DefaultIfEmpty()
                           orderby d.Name
                           select new DepartmentDto
                           {
                               Id = d.Id.ToString(),
                               Name = d.Name,
                               CompanyId = d.CompanyId != null ? d.CompanyId.ToString()! : string.Empty,
                               CompanyName = c != null ? c.Name : null
                           }).ToListAsync();

        return Ok(new ApiResponse<List<DepartmentDto>> { Success = true, Data = items });
    }
}
