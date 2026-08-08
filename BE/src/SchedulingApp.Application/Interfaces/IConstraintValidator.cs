using SchedulingApp.Application.DTOs;

namespace SchedulingApp.Application.Interfaces;

/// <summary>
/// Interface kiểm tra ràng buộc nghiệp vụ trước khi xếp ca.
///
/// ─── 7 RÀNG BUỘC CỨNG CẦN IMPLEMENT ──────────────────────────────────────
/// Tham chiếu: Báo cáo tổng kết Chương 2.4 — Mô hình ràng buộc.
///
/// (HC1) Trùng giờ (Hard Overlap):
///       Nhân viên không được phân công 2 ca có thời gian giao nhau.
///       → Validate_OverlappingShifts_ReturnsInvalid
///
/// (HC2) Ca qua đêm (Overnight):
///       Ca có EndTime <= StartTime (EndTime thuộc ngày hôm sau).
///       Khi tính thời gian, phải cộng thêm 1 ngày cho EndTime.
///       → Validate_OvernightShift_CalculatesRestCorrectly
///
/// (HC3) Nghỉ tối thiểu giữa 2 ca (Minimum Rest):
///       Thời gian nghỉ giữa ca trước và ca sau phải >= MinRestHours (VD: 8 giờ).
///       → Validate_RestTimeBelowMinimum_ReturnsInvalid
///
/// (HC4) Chứng chỉ hết hạn (Expired Certification):
///       Vị trí yêu cầu chứng chỉ (Position.RequiresCertificate = true):
///       EmployeeAssignment.CertificateExpiryDate phải > StartTime của ca.
///       → Validate_ExpiredCertification_ReturnsInvalid
///
/// (HC5) Nhân viên đang nghỉ phép (On Leave):
///       Ngày ca (Shift.StartTime.Date) phải không nằm trong kỳ nghỉ phép
///       của nhân viên (EmployeeLeave.StartDate – EmployeeLeave.EndDate).
///       → Validate_EmployeeOnLeave_ReturnsInvalid
///
/// (HC6) Vượt giờ tối đa trong tuần (Max Weekly Hours):
///       Tổng giờ đã xếp trong tuần + giờ của ca mới phải <= Employee.MaxHoursPerWeek.
///       → Validate_ExceedsMaxWeeklyHours_ReturnsInvalid
///
/// (HC7) Điều động chéo phòng ban (Cross-Department Transfer):
///       Chỉ cho phép nếu nhân viên có EmployeeAssignment cho CompanyId + DepartmentId của ca
///       hoặc có AllowCrossDept = true, và có đủ chứng chỉ. Không có Assignment/AllowCrossDept = không được xếp.
///       → Validate_CrossDepartmentWithSkill_ReturnsValid
///       → Validate_CrossDepartmentWithoutPermission_ReturnsInvalid
///
/// (HC8) Vị trí hợp lệ theo công ty (Company-Position validity):
///       Ca làm việc chỉ hợp lệ nếu cặp (Shift.CompanyId, Shift.PositionId) tồn tại
///       trong bảng CompanyPosition và có IsActive = true.
///       → Validate_CompanyPositionIsActive_ReturnsValid
///
/// ─── TÀI LIỆU ──────────────────────────────────────────────────────────────
/// Xem hướng dẫn đầy đủ tại: be/HUONG_DAN_CAM_THUAT_TOAN.md
/// Test skeleton đã có sẵn tại: tests/SchedulingApp.Tests/ConstraintValidatorTests.cs
/// </summary>
public interface IConstraintValidator
{
    /// <summary>
    /// Kiểm tra 1 yêu cầu phân công cụ thể có hợp lệ không.
    /// Trả về ValidationResult.IsValid = true nếu vượt qua tất cả 7 ràng buộc cứng.
    /// Trả về IsValid = false và ghi rõ lý do trong Errors nếu vi phạm.
    /// </summary>
    ValidationResult Validate(AssignRequest request);
}
