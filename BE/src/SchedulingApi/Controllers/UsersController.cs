using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Common;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly FaceAttendanceDbContext _context;

    public UsersController(FaceAttendanceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? departmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) 
                                  || u.Code.ToLower().Contains(term) 
                                  || u.Email.ToLower().Contains(term));
        }

        if (companyId.HasValue)
        {
            query = query.Where(u => u.CompanyAssignments.Any(ca => ca.CompanyId == companyId.Value && ca.IsActive));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(u => u.DepartmentAssignments.Any(da => da.DepartmentId == departmentId.Value && da.IsActive));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.CompanyAssignments)
                .ThenInclude(ca => ca.Company)
            .Include(u => u.DepartmentAssignments)
                .ThenInclude(da => da.Department)
            .ToListAsync();

        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Code = u.Code,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            AvatarUrl = u.AvatarUrl,
            DateOfBirth = u.DateOfBirth,
            Gender = u.Gender,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            CompanyAssignments = u.CompanyAssignments.Select(ca => new UserCompanyAssignmentDto
            {
                Id = ca.Id,
                UserId = ca.UserId,
                CompanyId = ca.CompanyId,
                CompanyName = ca.Company?.Name,
                CompanyCode = ca.Company?.Code,
                IsPrimaryCompany = ca.IsPrimaryCompany,
                IsActive = ca.IsActive
            }).ToList(),
            DepartmentAssignments = u.DepartmentAssignments.Select(da => new UserDepartmentAssignmentDto
            {
                Id = da.Id,
                UserId = da.UserId,
                CompanyId = da.CompanyId,
                CompanyName = da.Company?.Name,
                DepartmentId = da.DepartmentId,
                DepartmentName = da.Department?.Name,
                DepartmentCode = da.Department?.Code,
                IsPrimaryDepartment = da.IsPrimaryDepartment,
                IsActive = da.IsActive
            }).ToList()
        }).ToList();

        var result = new PagedResult<UserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(new ApiResponse<PagedResult<UserDto>> { Success = true, Data = result });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.CompanyAssignments)
                .ThenInclude(ca => ca.Company)
            .Include(u => u.DepartmentAssignments)
                .ThenInclude(da => da.Department)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Errors = new List<string> { "Không tìm thấy người dùng." }
            });
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Code = user.Code,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CompanyAssignments = user.CompanyAssignments.Select(ca => new UserCompanyAssignmentDto
            {
                Id = ca.Id,
                UserId = ca.UserId,
                CompanyId = ca.CompanyId,
                CompanyName = ca.Company?.Name,
                CompanyCode = ca.Company?.Code,
                IsPrimaryCompany = ca.IsPrimaryCompany,
                IsActive = ca.IsActive
            }).ToList(),
            DepartmentAssignments = user.DepartmentAssignments.Select(da => new UserDepartmentAssignmentDto
            {
                Id = da.Id,
                UserId = da.UserId,
                CompanyId = da.CompanyId,
                CompanyName = da.Company?.Name,
                DepartmentId = da.DepartmentId,
                DepartmentName = da.Department?.Name,
                DepartmentCode = da.Department?.Code,
                IsPrimaryDepartment = da.IsPrimaryDepartment,
                IsActive = da.IsActive
            }).ToList()
        };

        return Ok(new ApiResponse<UserDto> { Success = true, Data = dto });
    }
}
