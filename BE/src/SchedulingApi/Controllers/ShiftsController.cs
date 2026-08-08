using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ShiftsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FaceAttendanceDbContext _faceContext;
    private readonly IMapper _mapper;

    public ShiftsController(AppDbContext context, FaceAttendanceDbContext faceContext, IMapper mapper)
    {
        _context = context;
        _faceContext = faceContext;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ShiftDto>>>> GetAll(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? companyId = null,
        [FromQuery] string? deptId = null)
    {
        var query = _context.Shifts.AsQueryable();
        if (!string.IsNullOrEmpty(companyId)) query = query.Where(s => s.CompanyId == companyId);
        if (!string.IsNullOrEmpty(deptId)) query = query.Where(s => s.DepartmentId == deptId);
        if (date.HasValue) query = query.Where(s => s.StartDate <= date.Value.Date && s.EndDate >= date.Value.Date);

        var items = await query.ToListAsync();

        var dtos = items.Select(s => {
            var dto = _mapper.Map<ShiftDto>(s);
            if (Guid.TryParse(s.CompanyId, out Guid cGuid))
                dto.CompanyName = _faceContext.Companies.FirstOrDefault(c => c.Id == cGuid)?.Name;
            if (Guid.TryParse(s.DepartmentId, out Guid dGuid))
                dto.DepartmentName = _faceContext.Departments.FirstOrDefault(d => d.Id == dGuid)?.Name;
            if (Guid.TryParse(s.PositionId, out Guid pGuid))
                dto.PositionName = _faceContext.JobTitles.FirstOrDefault(p => p.Id == pGuid)?.Name;
            return dto;
        }).ToList();

        return Ok(new ApiResponse<List<ShiftDto>> { Success = true, Data = dtos });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ShiftDto>>> GetById(int id)
    {
        var s = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == id);
        if (s == null) return NotFound(new ApiResponse<ShiftDto> { Success = false, Errors = new List<string> { "Không tìm thấy ca làm." } });
        
        var dto = _mapper.Map<ShiftDto>(s);
        if (Guid.TryParse(s.CompanyId, out Guid cGuid))
            dto.CompanyName = _faceContext.Companies.FirstOrDefault(c => c.Id == cGuid)?.Name;
        if (Guid.TryParse(s.DepartmentId, out Guid dGuid))
            dto.DepartmentName = _faceContext.Departments.FirstOrDefault(d => d.Id == dGuid)?.Name;
        if (Guid.TryParse(s.PositionId, out Guid pGuid))
            dto.PositionName = _faceContext.JobTitles.FirstOrDefault(p => p.Id == pGuid)?.Name;

        return Ok(new ApiResponse<ShiftDto> { Success = true, Data = dto });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShiftDto>>> Create([FromBody] ShiftDto dto)
    {
        var entity = new Shift
        {
            MasterShiftId = dto.MasterShiftId,
            CompanyId = dto.CompanyId ?? string.Empty,
            DepartmentId = dto.DepartmentId ?? string.Empty,
            PositionId = dto.PositionId ?? string.Empty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RequiredEmployeeCount = dto.RequiredEmployeeCount
        };
        _context.Shifts.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<ShiftDto> { Success = true, Data = _mapper.Map<ShiftDto>(entity) });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ShiftDto>>> Update(int id, [FromBody] ShiftDto dto)
    {
        var entity = await _context.Shifts.FindAsync(id);
        if (entity == null) return NotFound(new ApiResponse<ShiftDto> { Success = false, Errors = new List<string> { "Không tìm thấy ca làm." } });
        
        entity.MasterShiftId = dto.MasterShiftId;
        entity.CompanyId = dto.CompanyId ?? string.Empty;
        entity.DepartmentId = dto.DepartmentId ?? string.Empty;
        entity.PositionId = dto.PositionId ?? string.Empty;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.RequiredEmployeeCount = dto.RequiredEmployeeCount;
        
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<ShiftDto> { Success = true, Data = _mapper.Map<ShiftDto>(entity) });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var entity = await _context.Shifts.FindAsync(id);
        if (entity == null) return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { "Không tìm thấy ca làm." } });
        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<object> { Success = true });
    }
}
