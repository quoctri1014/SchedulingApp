using SchedulingApp.Application.DTOs;

namespace SchedulingApp.Application.Interfaces;

/// <summary>
/// Factory để resolve đúng Solver theo tên thuật toán.
/// Mỗi thành viên chỉ cần: (1) tạo file Solver riêng, (2) AddScoped trong Program.cs.
/// Không ai phải sửa Controller hay Factory.
/// </summary>
public interface ISolverFactory
{
    ISolver Resolve(string algorithmName);
    IEnumerable<string> GetAvailableAlgorithms();
}
