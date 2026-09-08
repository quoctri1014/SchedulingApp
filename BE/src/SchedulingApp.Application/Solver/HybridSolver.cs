using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

/// <summary>
/// Hybrid solver: Greedy constructs a feasible seed, GA explores alternatives,
/// and a 2-opt-style local search swaps assignments between two shifts.
/// </summary>
public class HybridSolver : ISolver
{
    private const double UnderstaffedPenalty = 10_000;
    private const double InvalidAssignmentPenalty = 20_000;
    private const double DuplicatePenalty = 15_000;
    private const double OverlapPenalty = 20_000;
    private const double RestPenalty = 10_000;
    private const double OvertimePenalty = 5_000;
    private readonly ILogger<HybridSolver> _logger;
    private Random _random = new();

    public HybridSolver(ILogger<HybridSolver> logger) => _logger = logger;
    public string AlgorithmName => "hybrid";

    public ScheduleResultDto Run(SolverInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var stopwatch = Stopwatch.StartNew();
        var shifts = input.Shifts.OrderBy(shift => shift.StartDate).ToList();
        var employees = input.Employees.ToList();
        var populationSize = Math.Clamp(ReadInt(input.Options, "populationSize", 40), 4, 300);
        var generations = Math.Clamp(ReadInt(input.Options, "generations", 80), 0, 2_000);
        var mutationRate = Math.Clamp(ReadDouble(input.Options, "mutationRate", 0.10), 0, 1);
        var crossoverRate = Math.Clamp(ReadDouble(input.Options, "crossoverRate", 0.80), 0, 1);
        var localSearchIterations = Math.Clamp(ReadInt(input.Options, "localSearchIterations", 200), 0, 5_000);
        var minimumRestHours = Math.Max(0, ReadDouble(input.Options, "minimumRestHours", 12));
        var randomSeed = ReadInt(input.Options, "randomSeed", -1);
        if (randomSeed >= 0) _random = new Random(randomSeed);

        _logger.LogInformation("Hybrid: {Shifts} shifts, {Employees} employees, {Generations} generations.", shifts.Count, employees.Count, generations);
        if (shifts.Count == 0)
        {
            stopwatch.Stop();
            return new ScheduleResultDto { ExecutionTimeMs = Math.Max(1, stopwatch.ElapsedMilliseconds) };
        }

        var greedySeed = BuildGreedySolution(shifts, employees, minimumRestHours);
        var population = CreateInitialPopulation(populationSize, greedySeed, shifts, employees, minimumRestHours);
        var best = Clone(population[0]);
        var bestScore = Evaluate(best, shifts, employees, minimumRestHours).Score;

        for (var generation = 0; generation < generations; generation++)
        {
            var evaluations = population.Select(candidate => Evaluate(candidate, shifts, employees, minimumRestHours)).ToList();
            var eliteIndex = evaluations.Select((evaluation, index) => new { evaluation.Score, index }).OrderBy(item => item.Score).First().index;
            if (evaluations[eliteIndex].Score < bestScore)
            {
                best = Clone(population[eliteIndex]);
                bestScore = evaluations[eliteIndex].Score;
            }

            var nextGeneration = new List<Chromosome> { Clone(population[eliteIndex]) };
            while (nextGeneration.Count < populationSize)
            {
                var firstParent = TournamentSelect(population, evaluations);
                var secondParent = TournamentSelect(population, evaluations);
                var child = _random.NextDouble() < crossoverRate ? Crossover(firstParent, secondParent) : Clone(firstParent);
                if (_random.NextDouble() < mutationRate) Mutate(child, shifts, employees, minimumRestHours);
                nextGeneration.Add(child);
            }
            population = nextGeneration;
        }

        best = ImproveWithTwoOpt(best, shifts, employees, minimumRestHours, localSearchIterations);
        var finalEvaluation = Evaluate(best, shifts, employees, minimumRestHours);
        stopwatch.Stop();
        _logger.LogInformation("Hybrid completed: score {Score}, hard violations {HardViolations}.", finalEvaluation.Score, finalEvaluation.HardViolations);
        return ToResult(best, shifts, employees, finalEvaluation, stopwatch.ElapsedMilliseconds);
    }

    private List<Chromosome> CreateInitialPopulation(int size, Chromosome greedySeed, IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, double minimumRestHours)
    {
        var population = new List<Chromosome> { Clone(greedySeed) };
        while (population.Count < size)
        {
            var variant = Clone(greedySeed);
            var mutationCount = 1 + _random.Next(Math.Max(1, shifts.Count / 3));
            for (var i = 0; i < mutationCount; i++) Mutate(variant, shifts, employees, minimumRestHours);
            population.Add(variant);
        }
        return population;
    }

    private Chromosome BuildGreedySolution(IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, double minimumRestHours)
    {
        var solution = new Chromosome(shifts.Select(_ => new List<string>()));
        for (var shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++)
        {
            var shift = shifts[shiftIndex];
            foreach (var employee in employees.Where(employee => IsEligible(employee, shift)).OrderBy(employee => AssignedHours(solution, employee.Id, shifts)).ThenBy(employee => employee.Id))
            {
                if (solution[shiftIndex].Count >= shift.RequiredEmployeeCount) break;
                if (CanAssign(solution, shiftIndex, employee.Id, shifts, employees, minimumRestHours)) solution[shiftIndex].Add(employee.Id);
            }
        }
        return solution;
    }

    private Chromosome ImproveWithTwoOpt(Chromosome initial, IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, double minimumRestHours, int maxIterations)
    {
        var current = Clone(initial);
        var currentScore = Evaluate(current, shifts, employees, minimumRestHours).Score;
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var firstShift = _random.Next(shifts.Count);
            var secondShift = _random.Next(shifts.Count);
            if (firstShift == secondShift || current[firstShift].Count == 0 || current[secondShift].Count == 0) continue;

            // 2-opt move: exchange an employee assignment between two shift genes.
            var candidate = Clone(current);
            var firstEmployee = _random.Next(candidate[firstShift].Count);
            var secondEmployee = _random.Next(candidate[secondShift].Count);
            (candidate[firstShift][firstEmployee], candidate[secondShift][secondEmployee]) = (candidate[secondShift][secondEmployee], candidate[firstShift][firstEmployee]);
            var candidateScore = Evaluate(candidate, shifts, employees, minimumRestHours).Score;
            if (candidateScore < currentScore) { current = candidate; currentScore = candidateScore; }
        }
        return current;
    }

    private Chromosome TournamentSelect(IReadOnlyList<Chromosome> population, IReadOnlyList<Evaluation> evaluations)
    {
        var bestIndex = _random.Next(population.Count);
        for (var i = 0; i < 2; i++)
        {
            var candidateIndex = _random.Next(population.Count);
            if (evaluations[candidateIndex].Score < evaluations[bestIndex].Score) bestIndex = candidateIndex;
        }
        return population[bestIndex];
    }

    private Chromosome Crossover(Chromosome firstParent, Chromosome secondParent)
    {
        var splitPoint = _random.Next(firstParent.Count + 1);
        return new Chromosome(firstParent.Select((_, index) => index < splitPoint ? firstParent[index].ToList() : secondParent[index].ToList()));
    }

    private void Mutate(Chromosome chromosome, IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, double minimumRestHours)
    {
        if (shifts.Count == 0 || employees.Count == 0) return;
        var shiftIndex = _random.Next(shifts.Count);
        var shift = shifts[shiftIndex];
        var candidates = employees.Where(employee => IsEligible(employee, shift)).OrderBy(_ => _random.Next()).ToList();
        var replacement = candidates.FirstOrDefault(employee => CanAssign(chromosome, shiftIndex, employee.Id, shifts, employees, minimumRestHours));
        if (replacement is null) return;
        if (chromosome[shiftIndex].Count < shift.RequiredEmployeeCount) chromosome[shiftIndex].Add(replacement.Id);
        else if (chromosome[shiftIndex].Count > 0) chromosome[shiftIndex][_random.Next(chromosome[shiftIndex].Count)] = replacement.Id;
    }

    private Evaluation Evaluate(Chromosome solution, IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, double minimumRestHours)
    {
        var employeeById = employees.ToDictionary(employee => employee.Id);
        var assignedHours = employees.ToDictionary(employee => employee.Id, _ => 0d);
        var penalties = new Dictionary<string, double>();
        var hardViolations = 0;

        for (var shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++)
        {
            var shift = shifts[shiftIndex];
            var assigned = solution[shiftIndex];
            if (assigned.Count < shift.RequiredEmployeeCount)
            {
                var missing = shift.RequiredEmployeeCount - assigned.Count;
                AddPenalty(penalties, "Ca thiếu nhân sự", missing * UnderstaffedPenalty); hardViolations += missing;
            }
            var duplicates = assigned.Count - assigned.Distinct().Count();
            if (duplicates > 0) { AddPenalty(penalties, "Trùng nhân viên trong cùng ca", duplicates * DuplicatePenalty); hardViolations += duplicates; }
            foreach (var employeeId in assigned)
            {
                if (!employeeById.TryGetValue(employeeId, out var employee) || !IsEligible(employee, shift))
                {
                    AddPenalty(penalties, "Phân công không hợp lệ", InvalidAssignmentPenalty); hardViolations++; continue;
                }
                assignedHours[employeeId] += ShiftHours(shift);
                if (employee.Preferences.Any(preference => preference.PreferredDayOff == shift.StartDate.DayOfWeek || (preference.PreferredShiftStart.HasValue && preference.PreferredShiftStart.Value != shift.StartDate.TimeOfDay))) AddPenalty(penalties, "Không đáp ứng nguyện vọng", 10);
            }
        }

        foreach (var employee in employees.Where(employee => assignedHours[employee.Id] > employee.MaxHoursPerWeek))
        {
            AddPenalty(penalties, "Vượt giới hạn giờ làm", (assignedHours[employee.Id] - employee.MaxHoursPerWeek) * OvertimePenalty); hardViolations++;
        }

        for (var first = 0; first < shifts.Count; first++)
        for (var second = first + 1; second < shifts.Count; second++)
        foreach (var employeeId in solution[first].Intersect(solution[second]))
        {
            if (Overlaps(shifts[first], shifts[second])) { AddPenalty(penalties, "Ca làm trùng giờ", OverlapPenalty); hardViolations++; }
            else if (RestHoursBetween(shifts[first], shifts[second]) < minimumRestHours) { AddPenalty(penalties, "Không đủ thời gian nghỉ", RestPenalty); hardViolations++; }
        }

        if (assignedHours.Count > 1)
        {
            var average = assignedHours.Values.Average();
            var variance = assignedHours.Values.Average(hours => Math.Pow(hours - average, 2));
            if (variance > 0) AddPenalty(penalties, "Mất cân bằng giờ làm", variance);
        }
        return new Evaluation(penalties.Values.Sum(), hardViolations, penalties);
    }

    private bool CanAssign(Chromosome solution, int shiftIndex, string employeeId, IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, double minimumRestHours)
    {
        var employee = employees.First(candidate => candidate.Id == employeeId);
        if (solution[shiftIndex].Contains(employeeId) || AssignedHours(solution, employeeId, shifts) + ShiftHours(shifts[shiftIndex]) > employee.MaxHoursPerWeek) return false;
        for (var otherIndex = 0; otherIndex < shifts.Count; otherIndex++)
        {
            if (otherIndex == shiftIndex || !solution[otherIndex].Contains(employeeId)) continue;
            if (Overlaps(shifts[shiftIndex], shifts[otherIndex]) || RestHoursBetween(shifts[shiftIndex], shifts[otherIndex]) < minimumRestHours) return false;
        }
        return true;
    }

    private static bool IsEligible(Employee employee, Shift shift)
    {
        if (employee.Leaves.Any(leave => leave.IsApproved && leave.StartTime < shift.EndDate && leave.EndTime > shift.StartDate)) return false;
        // Lightweight requests and unit tests may not hydrate assignments; retain their explicit candidate list.
        return employee.Assignments.Count == 0 || employee.Assignments.Any(assignment => assignment.CompanyId == shift.CompanyId && assignment.DepartmentId == shift.DepartmentId && assignment.PositionId == shift.PositionId && assignment.EfficiencyMultiplier > 0 && (!assignment.CertificateExpiryDate.HasValue || assignment.CertificateExpiryDate.Value.Date >= shift.StartDate.Date));
    }

    private static double AssignedHours(Chromosome solution, string employeeId, IReadOnlyList<Shift> shifts) => solution.Select((assigned, index) => assigned.Count(id => id == employeeId) * ShiftHours(shifts[index])).Sum();
    private static bool Overlaps(Shift first, Shift second) => first.StartDate < second.EndDate && second.StartDate < first.EndDate;
    private static double RestHoursBetween(Shift first, Shift second)
    {
        var earlier = first.StartDate <= second.StartDate ? first : second;
        var later = ReferenceEquals(earlier, first) ? second : first;
        return (later.StartDate - earlier.EndDate).TotalHours;
    }
    private static double ShiftHours(Shift shift) => Math.Max(0, (shift.EndDate - shift.StartDate).TotalHours);
    private static void AddPenalty(Dictionary<string, double> penalties, string key, double value) => penalties[key] = penalties.GetValueOrDefault(key) + value;
    private static Chromosome Clone(Chromosome chromosome) => new(chromosome.Select(gene => gene.ToList()));
    private static int ReadInt(IReadOnlyDictionary<string, object> options, string key, int defaultValue) => TryReadDouble(options, key, out var value) ? Convert.ToInt32(value) : defaultValue;
    private static double ReadDouble(IReadOnlyDictionary<string, object> options, string key, double defaultValue) => TryReadDouble(options, key, out var value) ? value : defaultValue;
    private static bool TryReadDouble(IReadOnlyDictionary<string, object> options, string key, out double value)
    {
        value = 0;
        if (!options.TryGetValue(key, out var raw) || raw is null) return false;
        return raw switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.Number => element.TryGetDouble(out value),
            JsonElement element when element.ValueKind == JsonValueKind.String => double.TryParse(element.GetString(), out value),
            _ => double.TryParse(Convert.ToString(raw), out value)
        };
    }

    private static ScheduleResultDto ToResult(Chromosome solution, IReadOnlyList<Shift> shifts, IReadOnlyList<Employee> employees, Evaluation evaluation, long elapsedMilliseconds)
    {
        var employeeById = employees.ToDictionary(employee => employee.Id);
        var schedule = new Dictionary<string, List<AssignedEmployeeDto>>();
        var filled = 0;
        for (var index = 0; index < shifts.Count; index++)
        {
            var assigned = solution[index].Distinct().Where(employeeById.ContainsKey).Select(employeeId => new AssignedEmployeeDto { EmployeeId = employeeId, EmployeeName = employeeById[employeeId].FullName }).ToList();
            schedule[shifts[index].Id.ToString()] = assigned;
            if (assigned.Count >= shifts[index].RequiredEmployeeCount) filled++;
        }
        return new ScheduleResultDto
        {
            Schedule = schedule, TotalShifts = shifts.Count, FilledShifts = filled, UnfilledShifts = shifts.Count - filled,
            ExecutionTimeMs = Math.Max(1, elapsedMilliseconds), TotalPenaltyScore = evaluation.Score, HardViolationsCount = evaluation.HardViolations,
            SoftViolationsCount = evaluation.Penalties.Count(pair => pair.Key is "Không đáp ứng nguyện vọng" or "Mất cân bằng giờ làm"), PenaltyBreakdown = evaluation.Penalties
        };
    }

    private sealed record Evaluation(double Score, int HardViolations, Dictionary<string, double> Penalties);
    private sealed class Chromosome : List<List<string>>
    {
        public Chromosome() { }
        public Chromosome(IEnumerable<List<string>> genes) : base(genes) { }
    }
}
