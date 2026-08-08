namespace SchedulingApp.Domain.Entities.FaceAttendance;

public class User
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public Guid? RoleId { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<UserCompanyAssignment> CompanyAssignments { get; set; } = new List<UserCompanyAssignment>();
    public ICollection<UserDepartmentAssignment> DepartmentAssignments { get; set; } = new List<UserDepartmentAssignment>();
}
