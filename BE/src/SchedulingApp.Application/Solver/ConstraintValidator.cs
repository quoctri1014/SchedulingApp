using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;

namespace SchedulingApp.Application.Solver;

public class ConstraintValidator : IConstraintValidator
{
    public ValidationResult Validate(AssignRequest request)
    {
        var errors = new List<string>();
        var shiftEnd = NormalizeEnd(request.ShiftStart, request.ShiftEnd);
        var duration = shiftEnd - request.ShiftStart;

        foreach (var existing in request.ExistingShifts.Where(shift => shift.EmployeeId == request.EmployeeId))
        {
            var existingEnd = NormalizeEnd(existing.Start, existing.End);
            if (request.ShiftStart < existingEnd && existing.Start < shiftEnd)
                errors.Add("Nhân viên bị trùng giờ với một ca đã được phân công.");

            var restHours = (request.ShiftStart - existingEnd).TotalHours;
            if (restHours >= 0 && restHours < request.MinRestHours)
                errors.Add($"Thời gian nghỉ giữa hai ca phải đạt ít nhất {request.MinRestHours} giờ.");
        }

        if (request.PositionRequiresCertificate &&
            (!request.CertificateExpiryDate.HasValue || request.CertificateExpiryDate.Value <= request.ShiftStart))
            errors.Add("Chứng chỉ của nhân viên đã hết hạn hoặc chưa được cung cấp.");

        if (request.LeavePeriods.Any(leave => leave.IsApproved && request.ShiftStart < leave.End && leave.Start < shiftEnd))
            errors.Add("Nhân viên đang trong thời gian nghỉ phép đã được duyệt.");

        if (request.HoursAlreadyScheduledThisWeek + duration.TotalHours > request.MaxHoursPerWeek)
            errors.Add("Tổng số giờ làm trong tuần vượt quá giới hạn của nhân viên.");

        var matchingAssignment = request.Assignments.FirstOrDefault(assignment =>
            assignment.CompanyId == request.CompanyId &&
            assignment.PositionId == request.PositionId &&
            (assignment.DepartmentId == request.DepartmentId || assignment.AllowCrossDepartment));

        if (matchingAssignment is null)
            errors.Add("Nhân viên không có assignment hợp lệ cho công ty, phòng ban và vị trí này.");

        if (request.PositionRequiresCertificate &&
            matchingAssignment is not null &&
            (!matchingAssignment.CertificateExpiryDate.HasValue || matchingAssignment.CertificateExpiryDate.Value <= request.ShiftStart))
            errors.Add("Assignment của nhân viên không có chứng chỉ còn hiệu lực.");

        if (!request.CompanyPositions.Any(position =>
                position.CompanyId == request.CompanyId &&
                position.PositionId == request.PositionId &&
                position.IsActive))
            errors.Add("Vị trí không tồn tại hoặc không còn active tại công ty.");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors.Distinct().ToList()
        };
    }

    private static DateTime NormalizeEnd(DateTime start, DateTime end)
    {
        return end <= start ? end.AddDays(1) : end;
    }
}