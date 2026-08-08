namespace SchedulingApp.Tests;

/// <summary>
/// Tests cho IConstraintValidator.
/// TODO: Mỗi test method dưới đây cần được implement khi có ConstraintValidator thật.
/// </summary>
public class ConstraintValidatorTests
{
    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_OverlappingShifts_ReturnsInvalid()
    {
        // Kiểm tra: nhân viên đã được phân công ca A, không được phân công ca B trùng giờ
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_OvernightShift_CalculatesRestCorrectly()
    {
        // Kiểm tra: ca qua đêm (end <= start) phải được xử lý thêm 1 ngày khi tính thời gian nghỉ
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_RestTimeBelowMinimum_ReturnsInvalid()
    {
        // Kiểm tra: thời gian nghỉ giữa 2 ca liên tiếp phải đạt tối thiểu (VD 8 giờ)
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_ExpiredCertification_ReturnsInvalid()
    {
        // Kiểm tra: chứng chỉ hành nghề của nhân viên đã hết hạn trước ngày ca làm
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_EmployeeOnLeave_ReturnsInvalid()
    {
        // Kiểm tra: nhân viên đang trong kỳ nghỉ phép, không được xếp ca
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_ExceedsMaxWeeklyHours_ReturnsInvalid()
    {
        // Kiểm tra: tổng giờ làm trong tuần vượt MaxHoursPerWeek
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_CrossDepartmentWithSkill_ReturnsValid()
    {
        // Kiểm tra: nhân viên được điều động chéo phòng ban và có đủ chứng chỉ/kỹ năng → hợp lệ
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_CrossDepartmentWithoutPermission_ReturnsInvalid()
    {
        // Kiểm tra: nhân viên bị điều động chéo phòng ban mà không có quyền → không hợp lệ
    }

    [Fact(Skip = "TODO: cài đặt logic thật cho IConstraintValidator")]
    public void Validate_CompanyPositionIsActive_ReturnsValid()
    {
        // (HC8) Kiểm tra: ca làm việc thuộc vị trí hợp lệ và đang active tại công ty
    }
}
