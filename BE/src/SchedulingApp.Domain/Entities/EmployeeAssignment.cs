namespace SchedulingApp.Domain.Entities;

public class EmployeeAssignment
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;

    public DateTime? CertificateExpiryDate { get; set; }

    /// <summary>Hợp đồng chính (primary) của nhân viên tại công ty này.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Thứ tự ưu tiên khi nhân viên đủ điều kiện cho NHIỀU ca cùng lúc.
    /// 1 = ưu tiên cao nhất. null = thấp nhất (mặc định).
    /// Xem chi tiết tại HUONG_DAN_CAM_THUAT_TOAN.md mục 1.2.
    /// </summary>
    public int? PriorityOrder { get; set; }

    /// <summary>
    /// Hiệu suất cá nhân của nhân viên ở vị trí/phòng ban này (0.0 đến 1.0).
    /// Mặc định 1.0 = 100%. Nếu bằng 0 thì xem như không thể làm.
    /// Xem chi tiết tại HUONG_DAN_CAM_THUAT_TOAN.md mục 2.2.
    /// </summary>
    public float EfficiencyMultiplier { get; set; } = 1.0f;
}
