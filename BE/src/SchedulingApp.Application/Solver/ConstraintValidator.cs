using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

/// <summary>Kiểm tra các ràng buộc cứng trước khi gán một nhân viên vào ca.</summary>
public sealed class ConstraintValidator : IConstraintValidator
{
    public ValidationResult Validate(AssignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();
        var employee = request.Employee;
        var shift = request.Shift;

        if (employee is null) errors.Add("Không tìm thấy dữ liệu nhân viên.");
        if (shift is null) errors.Add("Không tìm thấy dữ liệu ca làm việc.");
        if (employee is null || shift is null) return Result(errors);

        var normalizedShift = Normalize(shift);
        if (!request.CompanyPositionIsActive)
            errors.Add("Vị trí của ca không hoạt động tại công ty.");

        var matchingAssignments = employee.Assignments.Where(a =>
            a.CompanyId == shift.CompanyId && a.PositionId == shift.PositionId).ToList();
        var exactAssignment = matchingAssignments.FirstOrDefault(a => a.DepartmentId == shift.DepartmentId);
        var effectiveAssignment = exactAssignment ?? (request.AllowCrossDepartment ? matchingAssignments.FirstOrDefault() : null);
        if (effectiveAssignment is null)
            errors.Add("Nhân viên không có phân công phù hợp cho công ty, phòng ban và vị trí của ca.");

        if (request.RequiresCertificate &&
            (effectiveAssignment?.CertificateExpiryDate is null || effectiveAssignment.CertificateExpiryDate.Value.Date < normalizedShift.Start.Date))
            errors.Add("Chứng chỉ của nhân viên không còn hiệu lực tại ngày làm việc.");

        if (employee.Leaves.Any(leave => leave.IsApproved && leave.StartTime < normalizedShift.End && leave.EndTime > normalizedShift.Start))
            errors.Add("Nhân viên đang trong thời gian nghỉ phép đã được duyệt.");

        foreach (var existing in request.ExistingAssignedShifts)
        {
            var normalizedExisting = Normalize(existing);
            if (Overlaps(normalizedShift, normalizedExisting))
            {
                errors.Add($"Ca làm việc bị trùng giờ với ca {existing.Id}.");
                continue;
            }

            if (RestHours(normalizedShift, normalizedExisting) < Math.Max(0, request.MinimumRestHours))
                errors.Add($"Thời gian nghỉ giữa ca mới và ca {existing.Id} nhỏ hơn {request.MinimumRestHours:0.##} giờ.");
        }

        var weekStart = normalizedShift.Start.Date.AddDays(-(((int)normalizedShift.Start.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(7);
        var existingHours = request.ExistingAssignedShifts
            .Select(Normalize)
            .Where(period => period.Start >= weekStart && period.Start < weekEnd)
            .Sum(period => (period.End - period.Start).TotalHours);
        var proposedHours = (normalizedShift.End - normalizedShift.Start).TotalHours;
        if (existingHours + proposedHours > employee.MaxHoursPerWeek)
            errors.Add($"Tổng giờ làm trong tuần ({existingHours + proposedHours:0.##}) vượt giới hạn {employee.MaxHoursPerWeek} giờ.");

        return Result(errors);
    }

    private static ValidationResult Result(List<string> errors) => new() { IsValid = errors.Count == 0, Errors = errors.Distinct().ToList() };

    private static (DateTime Start, DateTime End) Normalize(Shift shift)
    {
        var end = shift.EndDate <= shift.StartDate ? shift.EndDate.AddDays(1) : shift.EndDate;
        return (shift.StartDate, end);
    }

    private static bool Overlaps((DateTime Start, DateTime End) first, (DateTime Start, DateTime End) second) =>
        first.Start < second.End && second.Start < first.End;

    private static double RestHours((DateTime Start, DateTime End) first, (DateTime Start, DateTime End) second)
    {
        var earlier = first.Start <= second.Start ? first : second;
        var later = first.Start <= second.Start ? second : first;
        return Math.Max(0, (later.Start - earlier.End).TotalHours);
    }
}
