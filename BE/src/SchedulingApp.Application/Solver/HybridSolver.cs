using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

/// <summary>
/// Hybrid solver: Greedy constructs a feasible seed, GA explores alternatives,
/// and a conflict-driven 2-opt local search with delta evaluation refines assignments.
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

    // Technique ②: ThreadLocal<Random> ensures thread-safety when evaluating or mutating across parallel threads
    private static int _seedCounter = Environment.TickCount;
    private static readonly ThreadLocal<Random> ThreadRandom = new(() => new Random(Interlocked.Increment(ref _seedCounter)));

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

        var history = new List<ConvergencePointDto>();
        var greedySeed = BuildGreedySolution(shifts, employees, minimumRestHours);
        var population = CreateInitialPopulation(populationSize, greedySeed, shifts, employees, minimumRestHours);

        // Technique ②: Parallel evaluation for initial population to avoid redundant single-thread evaluation
        var initialEvaluations = population
            .AsParallel()
            .AsOrdered()
            .Select(candidate => Evaluate(candidate, shifts, employees, minimumRestHours))
            .ToList();

        var bestIdx = 0;
        for (var i = 1; i < initialEvaluations.Count; i++)
        {
            if (initialEvaluations[i].Score < initialEvaluations[bestIdx].Score) bestIdx = i;
        }

        var best = Clone(population[bestIdx]);
        var bestScore = initialEvaluations[bestIdx].Score;

        history.Add(new ConvergencePointDto
        {
            Iteration = 0,
            BestPenalty = Math.Round(bestScore, 2),
            AvgPenalty = Math.Round(initialEvaluations.Average(e => e.Score), 2)
        });

        // Technique ③: Adaptive Early Stopping tracking variables
        var noImprovementCount = 0;
        var bestFitnessLastCheck = bestScore;
        string? earlyStoppingReason = null;
        int completedGenerations = generations;

        for (var generation = 1; generation <= generations; generation++)
        {
            // Technique ②: Parallel fitness evaluation using PLINQ (.AsParallel().AsOrdered())
            var evaluations = population
                .AsParallel()
                .AsOrdered()
                .Select(candidate => Evaluate(candidate, shifts, employees, minimumRestHours))
                .ToList();

            var eliteIndex = 0;
            for (var i = 1; i < evaluations.Count; i++)
            {
                if (evaluations[i].Score < evaluations[eliteIndex].Score) eliteIndex = i;
            }

            if (evaluations[eliteIndex].Score < bestScore)
            {
                best = Clone(population[eliteIndex]);
                bestScore = evaluations[eliteIndex].Score;
            }

            if (generation % 5 == 0 || generation == 1 || generation == generations)
            {
                history.Add(new ConvergencePointDto
                {
                    Iteration = generation,
                    BestPenalty = Math.Round(bestScore, 2),
                    AvgPenalty = Math.Round(evaluations.Average(e => e.Score), 2)
                });
            }

            // Technique ③: Adaptive Early Stopping conditions (Ngưỡng nới: noImprovementCount = 20, threshold = 0.001%)
            if (bestScore == 0)
            {
                completedGenerations = generation;
                earlyStoppingReason = $"Đã đạt nghiệm tối ưu (Penalty = 0) tại thế hệ {generation}.";
                _logger.LogInformation("Early stopping: {Reason}", earlyStoppingReason);
                break;
            }

            var relativeImprovement = bestFitnessLastCheck > 0
                ? (bestFitnessLastCheck - bestScore) / bestFitnessLastCheck
                : 0;

            if (relativeImprovement > 0.00001) // Cải thiện > 0.001% (ngưỡng nới)
            {
                bestFitnessLastCheck = bestScore;
                noImprovementCount = 0;
            }
            else
            {
                noImprovementCount++;
                if (noImprovementCount >= 20) // 20 thế hệ liên tiếp
                {
                    completedGenerations = generation;
                    earlyStoppingReason = $"Không cải thiện > 0.001% trong 20 thế hệ liên tiếp (hội tụ tại thế hệ {generation}/{generations}).";
                    _logger.LogInformation("Early stopping: {Reason}", earlyStoppingReason);
                    break;
                }
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

        // Techniques ① & ④: Conflict-driven 2-Opt with Delta Evaluation
        best = ImproveWithTwoOpt(best, shifts, employees, minimumRestHours, localSearchIterations);
        var finalEvaluation = Evaluate(best, shifts, employees, minimumRestHours);
        if (history.Count > 0 && finalEvaluation.Score < history[^1].BestPenalty)
        {
            history.Add(new ConvergencePointDto
            {
                Iteration = completedGenerations + (localSearchIterations > 0 ? 1 : 0),
                BestPenalty = Math.Round(finalEvaluation.Score, 2),
                AvgPenalty = Math.Round(finalEvaluation.Score, 2)
            });
        }
        stopwatch.Stop();
        _logger.LogInformation("Hybrid completed: score {Score}, hard violations {HardViolations}.", finalEvaluation.Score, finalEvaluation.HardViolations);
        return ToResult(best, shifts, employees, finalEvaluation, stopwatch.ElapsedMilliseconds, history, earlyStoppingReason, completedGenerations);
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

    /// <summary>
    /// Local search 2-Opt combining:
    /// ④ Conflict-driven shift selection (focusing only on shifts with violations)
    /// ① Incremental delta evaluation (O(1)/O(k) cost calculation without full evaluation or chromosome copying)
    /// </summary>
    private Chromosome ImproveWithTwoOpt(
        Chromosome initial,
        IReadOnlyList<Shift> shifts,
        IReadOnlyList<Employee> employees,
        double minimumRestHours,
        int maxIterations)
    {
        if (maxIterations <= 0 || shifts.Count < 2) return initial;

        var current = Clone(initial);
        var currentEval = Evaluate(current, shifts, employees, minimumRestHours);
        var currentScore = currentEval.Score;
        if (currentScore == 0) return current;

        // Precompute state for incremental delta evaluation
        var employeeById = employees.ToDictionary(e => e.Id);
        var assignedHours = employees.ToDictionary(e => e.Id, _ => 0d);
        var shiftsByEmployee = employees.ToDictionary(e => e.Id, _ => new HashSet<int>());

        for (var s = 0; s < shifts.Count; s++)
        {
            var hours = ShiftHours(shifts[s]);
            foreach (var empId in current[s])
            {
                if (assignedHours.ContainsKey(empId))
                    assignedHours[empId] += hours;
                if (shiftsByEmployee.ContainsKey(empId))
                    shiftsByEmployee[empId].Add(s);
            }
        }

        // ④ Conflict-driven 2-Opt: build candidateShifts containing only shifts with violations
        var candidateShifts = GetConflictShiftIndices(current, shifts, employees, minimumRestHours);
        if (candidateShifts.Count == 0 && currentScore <= 0) return current;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            int firstShift, secondShift;
            if (candidateShifts.Count > 0)
            {
                firstShift = candidateShifts[_random.Next(candidateShifts.Count)];
                if (candidateShifts.Count > 1 && _random.NextDouble() < 0.5)
                {
                    secondShift = candidateShifts[_random.Next(candidateShifts.Count)];
                }
                else
                {
                    secondShift = _random.Next(shifts.Count);
                }
                if (firstShift == secondShift) continue;
            }
            else
            {
                firstShift = _random.Next(shifts.Count);
                secondShift = _random.Next(shifts.Count);
                if (firstShift == secondShift) continue;
            }

            if (current[firstShift].Count == 0 || current[secondShift].Count == 0) continue;

            var firstEmpIdx = _random.Next(current[firstShift].Count);
            var secondEmpIdx = _random.Next(current[secondShift].Count);
            var empA = current[firstShift][firstEmpIdx];
            var empB = current[secondShift][secondEmpIdx];
            if (empA == empB) continue;

            // ① Delta Evaluation: evaluate penalty change locally without cloning or full Evaluate()
            var delta = EvaluateDelta(
                current, firstShift, secondShift, firstEmpIdx, secondEmpIdx,
                empA, empB, shifts, employeeById, assignedHours, shiftsByEmployee, minimumRestHours);

            if (delta < -1e-6)
            {
                // Accept move: swap in-place
                current[firstShift][firstEmpIdx] = empB;
                current[secondShift][secondEmpIdx] = empA;

                // Update incremental state
                var hoursA = ShiftHours(shifts[firstShift]);
                var hoursB = ShiftHours(shifts[secondShift]);
                assignedHours[empA] += (hoursB - hoursA);
                assignedHours[empB] += (hoursA - hoursB);

                if (!current[firstShift].Contains(empA)) shiftsByEmployee[empA].Remove(firstShift);
                shiftsByEmployee[empA].Add(secondShift);
                if (!current[secondShift].Contains(empB)) shiftsByEmployee[empB].Remove(secondShift);
                shiftsByEmployee[empB].Add(firstShift);

                currentScore += delta;
                if (currentScore <= 0) break;

                // Refresh conflict list upon improvement
                candidateShifts = GetConflictShiftIndices(current, shifts, employees, minimumRestHours);
                if (candidateShifts.Count == 0) break;
            }
        }
        return current;
    }

    /// <summary>
    /// ① Delta Evaluation: Calculates the change in total penalty resulting from swapping empA and empB.
    /// Evaluates only local constraints: duplicates, eligibility, preferences, overtime, and shift-to-shift overlap/rest.
    /// Complexity: O(k) where k is the number of shifts assigned to empA and empB (k &lt;&lt; S).
    /// </summary>
    private double EvaluateDelta(
        Chromosome solution,
        int shiftA,
        int shiftB,
        int empIdxA,
        int empIdxB,
        string empA,
        string empB,
        IReadOnlyList<Shift> shifts,
        IReadOnlyDictionary<string, Employee> employeeById,
        IReadOnlyDictionary<string, double> assignedHours,
        IReadOnlyDictionary<string, HashSet<int>> shiftsByEmployee,
        double minimumRestHours)
    {
        double deltaPenalty = 0;

        // 1. Duplicate employee penalty changes in shiftA and shiftB
        var countEmpAInShiftA = 0;
        var countEmpBInShiftA = 0;
        foreach (var id in solution[shiftA])
        {
            if (id == empA) countEmpAInShiftA++;
            else if (id == empB) countEmpBInShiftA++;
        }

        var countEmpBInShiftB = 0;
        var countEmpAInShiftB = 0;
        foreach (var id in solution[shiftB])
        {
            if (id == empB) countEmpBInShiftB++;
            else if (id == empA) countEmpAInShiftB++;
        }

        var dupChangeA = (countEmpBInShiftA >= 1 ? 1 : 0) - (countEmpAInShiftA >= 2 ? 1 : 0);
        var dupChangeB = (countEmpAInShiftB >= 1 ? 1 : 0) - (countEmpBInShiftB >= 2 ? 1 : 0);
        deltaPenalty += (dupChangeA + dupChangeB) * DuplicatePenalty;

        // 2. Eligibility & Preference penalty changes
        var hasEmpA = employeeById.TryGetValue(empA, out var eA);
        var hasEmpB = employeeById.TryGetValue(empB, out var eB);

        var eligA_on_sA = hasEmpA && IsEligible(eA!, shifts[shiftA]);
        var eligB_on_sA = hasEmpB && IsEligible(eB!, shifts[shiftA]);
        if (!eligB_on_sA) deltaPenalty += InvalidAssignmentPenalty;
        if (!eligA_on_sA) deltaPenalty -= InvalidAssignmentPenalty;

        var eligB_on_sB = hasEmpB && IsEligible(eB!, shifts[shiftB]);
        var eligA_on_sB = hasEmpA && IsEligible(eA!, shifts[shiftB]);
        if (!eligA_on_sB) deltaPenalty += InvalidAssignmentPenalty;
        if (!eligB_on_sB) deltaPenalty -= InvalidAssignmentPenalty;

        if (hasEmpA && HasPreferenceViolation(eA!, shifts[shiftA])) deltaPenalty -= 10;
        if (hasEmpB && HasPreferenceViolation(eB!, shifts[shiftA])) deltaPenalty += 10;
        if (hasEmpB && HasPreferenceViolation(eB!, shifts[shiftB])) deltaPenalty -= 10;
        if (hasEmpA && HasPreferenceViolation(eA!, shifts[shiftB])) deltaPenalty += 10;

        // 3. Overtime penalty changes
        var hoursShiftA = ShiftHours(shifts[shiftA]);
        var hoursShiftB = ShiftHours(shifts[shiftB]);

        if (hasEmpA)
        {
            var curHours = assignedHours[empA];
            var newHours = curHours - hoursShiftA + hoursShiftB;
            var curOt = Math.Max(0, curHours - eA!.MaxHoursPerWeek);
            var newOt = Math.Max(0, newHours - eA!.MaxHoursPerWeek);
            deltaPenalty += (newOt - curOt) * OvertimePenalty;
        }

        if (hasEmpB)
        {
            var curHours = assignedHours[empB];
            var newHours = curHours - hoursShiftB + hoursShiftA;
            var curOt = Math.Max(0, curHours - eB!.MaxHoursPerWeek);
            var newOt = Math.Max(0, newHours - eB!.MaxHoursPerWeek);
            deltaPenalty += (newOt - curOt) * OvertimePenalty;
        }

        // 4. Overlap & Rest penalty changes for empA
        if (shiftsByEmployee.TryGetValue(empA, out var sSetA))
        {
            foreach (var s in sSetA)
            {
                if (s == shiftA) continue;
                if (s == shiftB)
                {
                    deltaPenalty -= InteractionPenalty(shifts[shiftA], shifts[shiftB], minimumRestHours);
                }
                else
                {
                    deltaPenalty -= InteractionPenalty(shifts[shiftA], shifts[s], minimumRestHours);
                    deltaPenalty += InteractionPenalty(shifts[shiftB], shifts[s], minimumRestHours);
                }
            }
        }

        // 4b. Overlap & Rest penalty changes for empB
        if (shiftsByEmployee.TryGetValue(empB, out var sSetB))
        {
            foreach (var s in sSetB)
            {
                if (s == shiftB) continue;
                if (s == shiftA)
                {
                    deltaPenalty -= InteractionPenalty(shifts[shiftA], shifts[shiftB], minimumRestHours);
                }
                else
                {
                    deltaPenalty -= InteractionPenalty(shifts[shiftB], shifts[s], minimumRestHours);
                    deltaPenalty += InteractionPenalty(shifts[shiftA], shifts[s], minimumRestHours);
                }
            }
        }

        // 5. Workload variance delta (if shift hours differ)
        if (assignedHours.Count > 1 && Math.Abs(hoursShiftA - hoursShiftB) > 1e-6)
        {
            var avg = assignedHours.Values.Average();
            var oldVarContrib = Math.Pow(assignedHours[empA] - avg, 2) + Math.Pow(assignedHours[empB] - avg, 2);
            var newHoursA = assignedHours[empA] - hoursShiftA + hoursShiftB;
            var newHoursB = assignedHours[empB] - hoursShiftB + hoursShiftA;
            var newVarContrib = Math.Pow(newHoursA - avg, 2) + Math.Pow(newHoursB - avg, 2);
            deltaPenalty += (newVarContrib - oldVarContrib) / assignedHours.Count;
        }

        return deltaPenalty;
    }

    /// <summary>
    /// ④ Conflict-driven 2-Opt candidate builder:
    /// Returns shift indices that participate in at least one constraint or preference violation.
    /// </summary>
    private static List<int> GetConflictShiftIndices(
        Chromosome solution,
        IReadOnlyList<Shift> shifts,
        IReadOnlyList<Employee> employees,
        double minimumRestHours)
    {
        var conflicts = new HashSet<int>();
        var employeeById = employees.ToDictionary(e => e.Id);
        var assignedHours = employees.ToDictionary(e => e.Id, _ => 0d);
        var shiftsByEmp = new Dictionary<string, List<int>>();

        for (var s = 0; s < shifts.Count; s++)
        {
            var shift = shifts[s];
            var assigned = solution[s];

            if (assigned.Count < shift.RequiredEmployeeCount)
                conflicts.Add(s);

            if (assigned.Count != assigned.Distinct().Count())
                conflicts.Add(s);

            foreach (var empId in assigned)
            {
                if (!employeeById.TryGetValue(empId, out var emp) || !IsEligible(emp, shift))
                {
                    conflicts.Add(s);
                    continue;
                }

                assignedHours[empId] += ShiftHours(shift);

                if (!shiftsByEmp.TryGetValue(empId, out var list))
                {
                    list = new List<int>();
                    shiftsByEmp[empId] = list;
                }
                list.Add(s);

                if (HasPreferenceViolation(emp, shift))
                    conflicts.Add(s);
            }
        }

        foreach (var (empId, hours) in assignedHours)
        {
            if (employeeById.TryGetValue(empId, out var emp) && hours > emp.MaxHoursPerWeek)
            {
                if (shiftsByEmp.TryGetValue(empId, out var sList))
                {
                    foreach (var sIdx in sList) conflicts.Add(sIdx);
                }
            }
        }

        foreach (var (_, sList) in shiftsByEmp)
        {
            for (var i = 0; i < sList.Count; i++)
            for (var j = i + 1; j < sList.Count; j++)
            {
                var s1 = shifts[sList[i]];
                var s2 = shifts[sList[j]];
                if (Overlaps(s1, s2) || RestHoursBetween(s1, s2) < minimumRestHours)
                {
                    conflicts.Add(sList[i]);
                    conflicts.Add(sList[j]);
                }
            }
        }

        return conflicts.OrderBy(x => x).ToList();
    }

    private static double InteractionPenalty(Shift s1, Shift s2, double minimumRestHours)
    {
        if (Overlaps(s1, s2)) return OverlapPenalty;
        if (RestHoursBetween(s1, s2) < minimumRestHours) return RestPenalty;
        return 0;
    }

    private static bool HasPreferenceViolation(Employee employee, Shift shift) =>
        employee.Preferences.Any(preference =>
            preference.PreferredDayOff == shift.StartDate.DayOfWeek ||
            (preference.PreferredShiftStart.HasValue && preference.PreferredShiftStart.Value != shift.StartDate.TimeOfDay));

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

    private static ScheduleResultDto ToResult(
        Chromosome solution,
        IReadOnlyList<Shift> shifts,
        IReadOnlyList<Employee> employees,
        Evaluation evaluation,
        long elapsedMilliseconds,
        List<ConvergencePointDto> convergenceHistory,
        string? earlyStoppingReason = null,
        int? completedGenerations = null)
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
            SoftViolationsCount = evaluation.Penalties.Count(pair => pair.Key is "Không đáp ứng nguyện vọng" or "Mất cân bằng giờ làm"), PenaltyBreakdown = evaluation.Penalties,
            ConvergenceHistory = convergenceHistory,
            EarlyStoppingReason = earlyStoppingReason,
            CompletedGenerations = completedGenerations
        };
    }

    private sealed record Evaluation(double Score, int HardViolations, Dictionary<string, double> Penalties);
    private sealed class Chromosome : List<List<string>>
    {
        public Chromosome() { }
        public Chromosome(IEnumerable<List<string>> genes) : base(genes) { }
    }
}
