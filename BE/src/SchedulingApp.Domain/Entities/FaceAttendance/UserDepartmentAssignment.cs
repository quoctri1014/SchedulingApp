namespace SchedulingApp.Domain.Entities.FaceAttendance;

public class UserDepartmentAssignment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? JobTitleId { get; set; }
    public Guid? DirectManagerUserId { get; set; }
    public bool IsPrimaryDepartment { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public User? User { get; set; }
    public FaceCompany? Company { get; set; }
    public FaceDepartment? Department { get; set; }
}
