using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;

namespace SchedulingApp.Application.Solver;

/// <summary>
/// STUB — Luôn trả về hợp lệ để hệ thống chạy được end-to-end.
/// TODO: Nhóm tự cài đặt logic kiểm tra thật ở đây (trùng giờ, chứng chỉ, nghỉ phép, giờ nghỉ tối thiểu...)
/// QUAN TRỌNG: KHÔNG thêm bất kỳ logic nghiệp vụ nào vào class này.
/// Tạo class Implement mới (ConstraintValidator.cs) và thay thế đăng ký DI trong Program.cs.
/// </summary>
public class FakeValidator : IConstraintValidator
{
    public ValidationResult Validate(AssignRequest request)
    {
        // TODO: nhóm tự cài đặt logic kiểm tra thật ở đây
        // Stub: luôn trả về hợp lệ
        return new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };
    }
}
