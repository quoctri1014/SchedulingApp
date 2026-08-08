namespace SchedulingApp.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<UserCompanyAssignmentDto> CompanyAssignments { get; set; } = new();
    public List<UserDepartmentAssignmentDto> DepartmentAssignments { get; set; } = new();
}

public class UserCompanyAssignmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyCode { get; set; }
    public bool IsPrimaryCompany { get; set; }
    public bool IsActive { get; set; }
}

public class UserDepartmentAssignmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentCode { get; set; }
    public bool IsPrimaryDepartment { get; set; }
    public bool IsActive { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
