using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;

namespace SchedulingApp.Application.Solver;

/// <summary>
/// Đăng ký tất cả ISolver thông qua DI (IEnumerable&lt;ISolver&gt;),
/// resolve theo AlgorithmName. Mỗi thành viên thêm solver vào DI là xong,
/// không cần sửa Factory hay Controller.
/// </summary>
public class SolverFactory : ISolverFactory
{
    private readonly IEnumerable<ISolver> _solvers;

    public SolverFactory(IEnumerable<ISolver> solvers)
    {
        _solvers = solvers;
    }

    public ISolver Resolve(string algorithmName)
    {
        var solver = _solvers.FirstOrDefault(s => s.AlgorithmName.Equals(algorithmName, StringComparison.OrdinalIgnoreCase));
        if (solver is null)
        {
            // Fallback về greedy nếu tên thuật toán không hợp lệ
            solver = _solvers.FirstOrDefault(s => s.AlgorithmName == "greedy")
                ?? throw new InvalidOperationException($"Không tìm thấy solver cho thuật toán '{algorithmName}'.");
        }
        return solver;
    }

    public IEnumerable<string> GetAvailableAlgorithms()
    {
        var registered = _solvers.Select(s => s.AlgorithmName.ToLower()).Distinct().ToList();
        var defaultAlgos = new[] { "greedy", "ga", "sa", "hybrid" };
        return defaultAlgos.Union(registered).Distinct();
    }
}
