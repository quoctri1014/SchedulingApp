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
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public EmployeesController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetAll()
    {
        var items = await _context.Employees.ToListAsync();
        return Ok(new ApiResponse<List<EmployeeDto>> { Success = true, Data = _mapper.Map<List<EmployeeDto>>(items) });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(string id)
    {
        var item = await _context.Employees.FindAsync(id);
        if (item == null) return NotFound(new ApiResponse<EmployeeDto> { Success = false, Errors = new List<string> { "Không tìm thấy nhân viên." } });
        return Ok(new ApiResponse<EmployeeDto> { Success = true, Data = _mapper.Map<EmployeeDto>(item) });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Create([FromBody] EmployeeDto dto)
    {
        var entity = new Employee { Id = dto.Id, FullName = dto.FullName, Email = dto.Email, PhoneNumber = dto.PhoneNumber, MaxHoursPerWeek = dto.MaxHoursPerWeek };
        _context.Employees.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<EmployeeDto> { Success = true, Data = _mapper.Map<EmployeeDto>(entity) });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Update(string id, [FromBody] EmployeeDto dto)
    {
        var entity = await _context.Employees.FindAsync(id);
        if (entity == null) return NotFound(new ApiResponse<EmployeeDto> { Success = false, Errors = new List<string> { "Không tìm thấy nhân viên." } });
        entity.FullName = dto.FullName;
        entity.Email = dto.Email;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.MaxHoursPerWeek = dto.MaxHoursPerWeek;
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<EmployeeDto> { Success = true, Data = _mapper.Map<EmployeeDto>(entity) });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var entity = await _context.Employees.FindAsync(id);
        if (entity == null) return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { "Không tìm thấy nhân viên." } });
        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<object> { Success = true });
    }

    // ─── Nested: Assignments ───────────────────────────────────────────────

    [HttpGet("{empId}/assignments")]
    public async Task<ActionResult<ApiResponse<List<EmployeeAssignmentDto>>>> GetAssignments(string empId)
    {
        var items = await _context.EmployeeAssignments
            .Where(a => a.EmployeeId == empId).ToListAsync();
        return Ok(new ApiResponse<List<EmployeeAssignmentDto>> { Success = true, Data = _mapper.Map<List<EmployeeAssignmentDto>>(items) });
    }

    [HttpPost("{empId}/assignments")]
    public async Task<ActionResult<ApiResponse<EmployeeAssignmentDto>>> CreateAssignment(string empId, [FromBody] EmployeeAssignmentDto dto)
    {
        var entity = new EmployeeAssignment
        {
            EmployeeId = empId,
            CompanyId = dto.CompanyId ?? string.Empty,
            DepartmentId = dto.DepartmentId ?? string.Empty,
            PositionId = dto.PositionId ?? string.Empty,
            CertificateExpiryDate = dto.CertificateExpiryDate,
            IsPrimary = dto.IsPrimary
        };
        _context.EmployeeAssignments.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<EmployeeAssignmentDto> { Success = true, Data = _mapper.Map<EmployeeAssignmentDto>(entity) });
    }

    [HttpDelete("{empId}/assignments/{assignmentId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAssignment(string empId, int assignmentId)
    {
        var entity = await _context.EmployeeAssignments.FindAsync(assignmentId);
        if (entity == null || entity.EmployeeId != empId) return NotFound();
        _context.EmployeeAssignments.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<object> { Success = true });
    }
}
