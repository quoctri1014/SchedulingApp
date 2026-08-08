using SchedulingApp.Application.DTOs;

namespace SchedulingApp.Application.Interfaces;

/// <summary>
/// Interface chung cho tất cả các thuật toán xếp ca.
/// 
/// ─── CÁCH IMPLEMENT ────────────────────────────────────────────────────────
/// 1. Tạo class mới trong Application/Solver/, implement interface này.
/// 2. Đặt AlgorithmName đúng key: "greedy" | "ga" | "sa" | "hybrid"
/// 3. Run() nhận SolverInput và trả về ScheduleResultDto (Schedule, TotalShifts...)
/// 4. Dùng ILogger để ghi log ra Console/File các quyết định của thuật toán.
/// </summary>
public interface ISolver
{
    string AlgorithmName { get; }
    ScheduleResultDto Run(SolverInput input);
}
