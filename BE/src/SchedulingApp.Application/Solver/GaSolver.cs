using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchedulingApp.Application.DTOs;
using SchedulingApp.Application.Interfaces;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Solver;

public class GaSolver : ISolver
{
    private const double MissingEmployeePenalty = 100.0;
    private const double UnknownEmployeePenalty = 10_000.0;
    private const double IneligibleEmployeePenalty = 10_000.0;
    private const double DuplicateInShiftPenalty = 10_000.0;
    private const double OverlapPenalty = 10_000.0;
    private const double MinRestPenaltyPerHour = 500.0;
    private const double WeeklyOvertimePenaltyPerHour = 1_000.0;
    private const double ImbalancePenaltyPerHour = 5.0;

    private readonly ILogger<GaSolver> _logger;

    public GaSolver(ILogger<GaSolver> logger)
    {
        _logger = logger;
    }

    public string AlgorithmName => "ga";

    public ScheduleResultDto Run(SolverInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateInputCollections(input);

        var stopwatch = Stopwatch.StartNew();
        var options = ReadOptions(input.Options);
        var targetShifts = NormalizeShifts(input.Shifts);
        var employees = NormalizeEmployees(input.Employees);
        ValidateUniqueIds(targetShifts, employees);
        var random = new Random(options.Seed);
        var population = InitializePopulation(options, targetShifts, employees, random);
        EvaluatePopulation(population, input, options);
        SortPopulationByFitness(population);
        population = EvolvePopulation(population, input, targetShifts, employees, options, random);
        if (population.Count == 0)
        {
            throw new InvalidOperationException("Cannot select best chromosome because the final GA population is empty.");
        }

        var bestChromosome = population[0];
        if (bestChromosome.Fitness == null)
        {
            throw new InvalidOperationException("Cannot select best chromosome because it is missing fitness.");
        }

        stopwatch.Stop();
        var result = MapToScheduleResult(bestChromosome, targetShifts, employees, stopwatch.ElapsedMilliseconds);

        _logger.LogInformation(
            "Algorithm {Algorithm} completed. PopulationSize={PopulationSize}, Generations={Generations}, " +
            "TotalShifts={TotalShifts}, FilledShifts={FilledShifts}, UnfilledShifts={UnfilledShifts}, " +
            "HardViolations={HardViolations}, SoftViolations={SoftViolations}, " +
            "TotalPenalty={TotalPenalty}, ExecutionTimeMs={ExecutionTimeMs}",
            AlgorithmName,
            options.PopulationSize,
            options.Generations,
            result.TotalShifts,
            result.FilledShifts,
            result.UnfilledShifts,
            result.HardViolationsCount,
            result.SoftViolationsCount,
            result.TotalPenaltyScore,
            result.ExecutionTimeMs);

        return result;
    }

    private sealed class Chromosome
    {
        public List<Gene> Genes { get; } = new();
        public FitnessResult? Fitness { get; set; }
    }

    private sealed class Gene
    {
        public int ShiftId { get; init; }
        public DateTime StartDate { get; init; }
        public int RequiredEmployeeCount { get; init; }
        public List<string> AssignedEmployeeIds { get; } = new();
    }

    private readonly record struct ShiftInterval(
        int ShiftId,
        DateTime Start,
        DateTime End);

    private sealed class FitnessResult
    {
        public int HardViolationsCount { get; set; }
        public double HardPenalty { get; set; }
        public int SoftViolationsCount { get; set; }
        public double SoftPenalty { get; set; }
        public double TotalPenalty
        {
            get
            {
                if (!double.IsFinite(HardPenalty) || !double.IsFinite(SoftPenalty))
                {
                    return double.MaxValue;
                }

                if (SoftPenalty > 0 && HardPenalty > double.MaxValue - SoftPenalty)
                {
                    return double.MaxValue;
                }

                if (SoftPenalty < 0 && HardPenalty < -double.MaxValue - SoftPenalty)
                {
                    return double.MaxValue;
                }

                return HardPenalty + SoftPenalty;
            }
        }
        public Dictionary<string, double> PenaltyBreakdown { get; } = new();
        public string StableTieBreaker { get; set; } = string.Empty;
    }

    private static Chromosome CreateRandomChromosome(
        IReadOnlyList<Shift> normalizedShifts,
        IReadOnlyList<Employee> normalizedEmployees,
        Random random)
    {
        var chromosome = new Chromosome();

        foreach (var shift in normalizedShifts)
        {
            var gene = new Gene
            {
                ShiftId = shift.Id,
                StartDate = shift.StartDate,
                RequiredEmployeeCount = shift.RequiredEmployeeCount
            };

            var eligibleEmployees = normalizedEmployees
                .Where(employee => IsEligibleForShift(employee, shift))
                .ToList();
            var targetCount = Math.Max(0, shift.RequiredEmployeeCount);
            var assignCount = Math.Min(targetCount, eligibleEmployees.Count);

            if (assignCount > 0)
            {
                var candidates = eligibleEmployees.ToList();
                ShufflePrefix(candidates, assignCount, random);

                for (var i = 0; i < assignCount; i++)
                {
                    gene.AssignedEmployeeIds.Add(candidates[i].Id);
                }
            }

            chromosome.Genes.Add(gene);
        }

        return chromosome;
    }

    private static List<Chromosome> InitializePopulation(
        GaOptions options,
        IReadOnlyList<Shift> normalizedShifts,
        IReadOnlyList<Employee> normalizedEmployees,
        Random random)
    {
        var population = new List<Chromosome>(options.PopulationSize);
        for (var i = 0; i < options.PopulationSize; i++)
        {
            population.Add(CreateRandomChromosome(normalizedShifts, normalizedEmployees, random));
        }

        return population;
    }

    private static void EvaluatePopulation(
        IEnumerable<Chromosome> population,
        SolverInput input,
        GaOptions options)
    {
        foreach (var chromosome in population)
        {
            chromosome.Fitness = Evaluate(chromosome, input, options);
        }
    }

    private static void SortPopulationByFitness(List<Chromosome> population)
    {
        population.Sort((left, right) =>
        {
            if (left.Fitness == null)
            {
                throw new InvalidOperationException("Cannot sort GA population because a chromosome is missing fitness.");
            }

            if (right.Fitness == null)
            {
                throw new InvalidOperationException("Cannot sort GA population because a chromosome is missing fitness.");
            }

            return CompareFitness(left.Fitness, right.Fitness);
        });
    }

    private static Chromosome TournamentSelect(
        IReadOnlyList<Chromosome> population,
        int tournamentSize,
        Random random)
    {
        if (population.Count == 0)
        {
            throw new InvalidOperationException("Cannot run tournament selection because the GA population is empty.");
        }

        var candidateCount = Math.Min(tournamentSize, population.Count);
        var candidateIndexes = Enumerable.Range(0, population.Count).ToList();
        Chromosome? best = null;

        for (var i = 0; i < candidateCount; i++)
        {
            var selectedIndexPosition = random.Next(i, candidateIndexes.Count);
            (candidateIndexes[i], candidateIndexes[selectedIndexPosition]) = (candidateIndexes[selectedIndexPosition], candidateIndexes[i]);

            var candidate = population[candidateIndexes[i]];
            if (candidate.Fitness == null)
            {
                throw new InvalidOperationException("Cannot run tournament selection because a chromosome is missing fitness.");
            }

            if (best == null)
            {
                best = candidate;
                continue;
            }

            if (best.Fitness == null)
            {
                throw new InvalidOperationException("Cannot run tournament selection because a chromosome is missing fitness.");
            }

            if (CompareFitness(candidate.Fitness, best.Fitness) < 0)
            {
                best = candidate;
            }
        }

        return best ?? throw new InvalidOperationException("Cannot run tournament selection because no candidate was selected.");
    }

    private static Chromosome CloneForOffspring(Chromosome source)
    {
        var clone = new Chromosome();
        foreach (var sourceGene in source.Genes)
        {
            var gene = new Gene
            {
                ShiftId = sourceGene.ShiftId,
                StartDate = sourceGene.StartDate,
                RequiredEmployeeCount = sourceGene.RequiredEmployeeCount
            };

            gene.AssignedEmployeeIds.AddRange(sourceGene.AssignedEmployeeIds);
            clone.Genes.Add(gene);
        }

        return clone;
    }

    private static Chromosome CloneElite(Chromosome source)
    {
        if (source.Fitness == null)
        {
            throw new InvalidOperationException("Cannot clone elite chromosome because it is missing fitness.");
        }

        var clone = CloneForOffspring(source);
        clone.Fitness = CloneFitnessResult(source.Fitness);
        return clone;
    }

    private static List<Chromosome> CreateElitePopulation(
        IReadOnlyList<Chromosome> sortedPopulation,
        int elitismCount)
    {
        if (elitismCount < 0 || elitismCount > sortedPopulation.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elitismCount),
                "Elitism count must be between 0 and the population count.");
        }

        var elitePopulation = new List<Chromosome>(elitismCount);
        for (var i = 0; i < elitismCount; i++)
        {
            elitePopulation.Add(CloneElite(sortedPopulation[i]));
        }

        return elitePopulation;
    }

    private static (Chromosome First, Chromosome Second) CreateOffspringPair(
        IReadOnlyList<Chromosome> population,
        IReadOnlyList<Employee> normalizedEmployees,
        IReadOnlyDictionary<int, Shift> shiftById,
        GaOptions options,
        Random random)
    {
        var firstParent = TournamentSelect(population, options.TournamentSize, random);
        var secondParent = TournamentSelect(population, options.TournamentSize, random);
        var (firstChild, secondChild) = Crossover(firstParent, secondParent, options.CrossoverRate, random);

        Mutate(firstChild, normalizedEmployees, shiftById, options.MutationRate, random);
        Mutate(secondChild, normalizedEmployees, shiftById, options.MutationRate, random);

        firstChild.Fitness = null;
        secondChild.Fitness = null;
        return (firstChild, secondChild);
    }

    private static List<Chromosome> CreateNextGeneration(
        IReadOnlyList<Chromosome> sortedPopulation,
        IReadOnlyList<Employee> normalizedEmployees,
        IReadOnlyDictionary<int, Shift> shiftById,
        GaOptions options,
        Random random)
    {
        if (sortedPopulation.Count == 0)
        {
            throw new InvalidOperationException("Cannot create next generation because the GA population is empty.");
        }

        var nextGeneration = CreateElitePopulation(sortedPopulation, options.ElitismCount);
        while (nextGeneration.Count < options.PopulationSize)
        {
            var (firstChild, secondChild) = CreateOffspringPair(
                sortedPopulation,
                normalizedEmployees,
                shiftById,
                options,
                random);
            nextGeneration.Add(firstChild);

            if (nextGeneration.Count < options.PopulationSize)
            {
                nextGeneration.Add(secondChild);
            }
        }

        return nextGeneration;
    }

    private static List<Chromosome> EvolvePopulation(
        IReadOnlyList<Chromosome> initialPopulation,
        SolverInput input,
        IReadOnlyList<Shift> normalizedShifts,
        IReadOnlyList<Employee> normalizedEmployees,
        GaOptions options,
        Random random)
    {
        if (options.Generations == 0)
        {
            return initialPopulation.ToList();
        }

        var shiftById = normalizedShifts.ToDictionary(shift => shift.Id);
        var population = initialPopulation;
        for (var generation = 0; generation < options.Generations; generation++)
        {
            var nextGeneration = CreateNextGeneration(population, normalizedEmployees, shiftById, options, random);
            EvaluatePopulation(nextGeneration, input, options);
            SortPopulationByFitness(nextGeneration);
            population = nextGeneration;
        }

        return population.ToList();
    }

    private static ScheduleResultDto MapToScheduleResult(
        Chromosome bestChromosome,
        IReadOnlyList<Shift> normalizedShifts,
        IReadOnlyList<Employee> normalizedEmployees,
        long executionTimeMs)
    {
        if (bestChromosome.Fitness == null)
        {
            throw new InvalidOperationException("Cannot map GA result because the best chromosome is missing fitness.");
        }

        if (bestChromosome.Genes.Count != normalizedShifts.Count)
        {
            throw new InvalidOperationException("Cannot map GA result because chromosome gene count does not match shift count.");
        }

        var employeeById = normalizedEmployees.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var schedule = new Dictionary<string, List<AssignedEmployeeDto>>();
        var filledShifts = 0;

        for (var i = 0; i < normalizedShifts.Count; i++)
        {
            var shift = normalizedShifts[i];
            var gene = bestChromosome.Genes[i];
            if (gene.ShiftId != shift.Id)
            {
                throw new InvalidOperationException("Cannot map GA result because chromosome genes are not aligned with shifts.");
            }

            var assignedEmployees = new List<AssignedEmployeeDto>();
            var seenEmployeeIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var employeeId in gene.AssignedEmployeeIds)
            {
                if (!employeeById.TryGetValue(employeeId, out var employee))
                {
                    continue;
                }

                if (!IsEligibleForShift(employee, shift))
                {
                    continue;
                }

                if (!seenEmployeeIds.Add(employeeId))
                {
                    continue;
                }

                assignedEmployees.Add(new AssignedEmployeeDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName
                });
            }

            schedule[shift.Id.ToString()] = assignedEmployees;

            var requiredEmployeeCount = Math.Max(0, shift.RequiredEmployeeCount);
            if (assignedEmployees.Count >= requiredEmployeeCount)
            {
                filledShifts++;
            }
        }

        return new ScheduleResultDto
        {
            Schedule = schedule,
            TotalShifts = normalizedShifts.Count,
            FilledShifts = filledShifts,
            UnfilledShifts = normalizedShifts.Count - filledShifts,
            ExecutionTimeMs = executionTimeMs,
            TotalPenaltyScore = bestChromosome.Fitness.TotalPenalty,
            HardViolationsCount = bestChromosome.Fitness.HardViolationsCount,
            SoftViolationsCount = bestChromosome.Fitness.SoftViolationsCount,
            PenaltyBreakdown = new Dictionary<string, double>(bestChromosome.Fitness.PenaltyBreakdown, StringComparer.Ordinal),
            AlgorithmRunId = null
        };
    }

    private static (Chromosome First, Chromosome Second) Crossover(
        Chromosome firstParent,
        Chromosome secondParent,
        double crossoverRate,
        Random random)
    {
        ValidateCrossoverParents(firstParent, secondParent);

        if (random.NextDouble() >= crossoverRate)
        {
            return (CloneForOffspring(firstParent), CloneForOffspring(secondParent));
        }

        var firstChild = new Chromosome();
        var secondChild = new Chromosome();

        for (var i = 0; i < firstParent.Genes.Count; i++)
        {
            var keepParentOrder = random.Next(2) == 0;
            var firstSource = keepParentOrder ? firstParent.Genes[i] : secondParent.Genes[i];
            var secondSource = keepParentOrder ? secondParent.Genes[i] : firstParent.Genes[i];

            firstChild.Genes.Add(CloneGene(firstSource));
            secondChild.Genes.Add(CloneGene(secondSource));
        }

        return (firstChild, secondChild);
    }

    private static void ValidateCrossoverParents(Chromosome firstParent, Chromosome secondParent)
    {
        if (firstParent.Genes.Count != secondParent.Genes.Count)
        {
            throw new InvalidOperationException("Cannot run crossover because parent chromosomes have different gene counts.");
        }

        for (var i = 0; i < firstParent.Genes.Count; i++)
        {
            if (firstParent.Genes[i].ShiftId != secondParent.Genes[i].ShiftId)
            {
                throw new InvalidOperationException("Cannot run crossover because parent chromosomes are not aligned by ShiftId.");
            }
        }
    }

    private static Gene CloneGene(Gene source)
    {
        var clone = new Gene
        {
            ShiftId = source.ShiftId,
            StartDate = source.StartDate,
            RequiredEmployeeCount = source.RequiredEmployeeCount
        };

        clone.AssignedEmployeeIds.AddRange(source.AssignedEmployeeIds);
        return clone;
    }

    private static FitnessResult CloneFitnessResult(FitnessResult source)
    {
        var clone = new FitnessResult
        {
            HardViolationsCount = source.HardViolationsCount,
            HardPenalty = source.HardPenalty,
            SoftViolationsCount = source.SoftViolationsCount,
            SoftPenalty = source.SoftPenalty,
            StableTieBreaker = source.StableTieBreaker
        };

        foreach (var (key, value) in source.PenaltyBreakdown)
        {
            clone.PenaltyBreakdown[key] = value;
        }

        return clone;
    }

    private static void Mutate(
        Chromosome chromosome,
        IReadOnlyList<Employee> normalizedEmployees,
        IReadOnlyDictionary<int, Shift> shiftById,
        double mutationRate,
        Random random)
    {
        var changed = false;

        foreach (var gene in chromosome.Genes)
        {
            if (random.NextDouble() >= mutationRate)
            {
                continue;
            }

            if (!shiftById.TryGetValue(gene.ShiftId, out var shift))
            {
                throw new InvalidOperationException(
                    $"Cannot mutate gene because ShiftId '{gene.ShiftId}' was not found in the normalized shift lookup.");
            }

            if (gene.AssignedEmployeeIds.Count == 0)
            {
                continue;
            }

            var assignmentIndex = random.Next(gene.AssignedEmployeeIds.Count);
            var assignedIds = new HashSet<string>(gene.AssignedEmployeeIds, StringComparer.Ordinal);
            var replacementCandidates = normalizedEmployees
                .Where(employee =>
                    IsEligibleForShift(employee, shift) &&
                    !assignedIds.Contains(employee.Id))
                .ToList();

            if (replacementCandidates.Count == 0)
            {
                continue;
            }

            var replacement = replacementCandidates[random.Next(replacementCandidates.Count)];
            gene.AssignedEmployeeIds[assignmentIndex] = replacement.Id;
            changed = true;
        }

        if (changed)
        {
            chromosome.Fitness = null;
        }
    }

    private static FitnessResult Evaluate(Chromosome chromosome, SolverInput input, GaOptions options)
    {
        var result = new FitnessResult();
        var employees = NormalizeEmployees(input.Employees);
        var employeeById = employees.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var shiftById = NormalizeShifts(input.Shifts).ToDictionary(s => s.Id);
        var intervalsByEmployee = new Dictionary<string, List<ShiftInterval>>(StringComparer.Ordinal);
        var hoursByEmployee = employees.ToDictionary(e => e.Id, _ => 0.0, StringComparer.Ordinal);
        var weeklyHoursByEmployee = new Dictionary<(string EmployeeId, DateTime WeekStart), double>();

        foreach (var gene in GetNormalizedGenes(chromosome))
        {
            shiftById.TryGetValue(gene.ShiftId, out var shift);

            var assigned = gene.AssignedEmployeeIds;
            var duplicateCount = assigned.Count - assigned.Distinct(StringComparer.Ordinal).Count();
            if (duplicateCount > 0)
            {
                AddHardPenalty(
                    result,
                    "hard_duplicate_employee_in_shift",
                    duplicateCount,
                    duplicateCount * DuplicateInShiftPenalty);
            }

            var validDistinctEmployeeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var employeeId in assigned)
            {
                if (!employeeById.TryGetValue(employeeId, out var employee))
                {
                    AddHardPenalty(result, "hard_unknown_employee", 1, UnknownEmployeePenalty);
                    continue;
                }

                if (shift != null && !IsEligibleForShift(employee, shift))
                {
                    AddHardPenalty(
                        result,
                        "hard_ineligible_employee_for_shift",
                        1,
                        IneligibleEmployeePenalty);
                    continue;
                }

                validDistinctEmployeeIds.Add(employeeId);
            }

            var requiredEmployeeCount = shift?.RequiredEmployeeCount ?? gene.RequiredEmployeeCount;
            var missing = Math.Max(0, requiredEmployeeCount - validDistinctEmployeeIds.Count);
            if (missing > 0)
            {
                AddSoftPenalty(result, "soft_understaffed_shift", 1, missing * MissingEmployeePenalty);
            }

            if (shift == null)
            {
                continue;
            }

            var interval = NormalizeInterval(shift);
            var hours = GetFinitePositiveHours(interval);
            foreach (var employeeId in validDistinctEmployeeIds)
            {
                if (!intervalsByEmployee.TryGetValue(employeeId, out var intervals))
                {
                    intervals = new List<ShiftInterval>();
                    intervalsByEmployee[employeeId] = intervals;
                }

                intervals.Add(interval);
                hoursByEmployee[employeeId] = hoursByEmployee.GetValueOrDefault(employeeId) + hours;

                foreach (var (weekStart, weekHours) in SplitHoursByWeek(interval))
                {
                    var key = (employeeId, weekStart);
                    weeklyHoursByEmployee[key] = weeklyHoursByEmployee.GetValueOrDefault(key) + weekHours;
                }
            }
        }

        foreach (var intervals in intervalsByEmployee.Values)
        {
            var orderedIntervals = SortIntervals(intervals);
            AddOverlapPenalties(result, orderedIntervals);
            AddMinRestPenalties(result, MergeOverlappingIntervals(orderedIntervals), options.MinRestHours);
        }

        foreach (var ((employeeId, _), hours) in weeklyHoursByEmployee)
        {
            var maxHoursPerWeek = employeeById[employeeId].MaxHoursPerWeek;
            var overtimeHours = maxHoursPerWeek > 0
                ? hours - maxHoursPerWeek
                : hours;

            if (overtimeHours > 0)
            {
                AddHardPenalty(
                    result,
                    "hard_weekly_overtime",
                    1,
                    overtimeHours * WeeklyOvertimePenaltyPerHour);
            }
        }

        if (employees.Count > 0)
        {
            var totalHours = hoursByEmployee.Values.Sum();
            var average = totalHours / employees.Count;
            var imbalance = hoursByEmployee.Values.Sum(hours => Math.Abs(hours - average));

            if (imbalance > 0)
            {
                AddSoftPenalty(result, "soft_hours_imbalance", 1, imbalance * ImbalancePenaltyPerHour);
            }
        }

        result.StableTieBreaker = CreateStableTieBreaker(chromosome);
        EnsureFinite(result);
        return result;
    }

    private static int CompareFitness(FitnessResult left, FitnessResult right)
    {
        var hardViolationsComparison = left.HardViolationsCount.CompareTo(right.HardViolationsCount);
        if (hardViolationsComparison != 0)
        {
            return hardViolationsComparison;
        }

        var hardPenaltyComparison = left.HardPenalty.CompareTo(right.HardPenalty);
        if (hardPenaltyComparison != 0)
        {
            return hardPenaltyComparison;
        }

        var softPenaltyComparison = left.SoftPenalty.CompareTo(right.SoftPenalty);
        if (softPenaltyComparison != 0)
        {
            return softPenaltyComparison;
        }

        return string.CompareOrdinal(left.StableTieBreaker, right.StableTieBreaker);
    }

    private static IReadOnlyList<ShiftInterval> SortIntervals(IEnumerable<ShiftInterval> intervals)
    {
        return intervals
            .OrderBy(i => i.Start)
            .ThenBy(i => i.End)
            .ThenBy(i => i.ShiftId)
            .ToList();
    }

    private static void AddOverlapPenalties(FitnessResult result, IReadOnlyList<ShiftInterval> intervals)
    {
        for (var i = 0; i < intervals.Count; i++)
        {
            for (var j = i + 1; j < intervals.Count; j++)
            {
                if (intervals[i].End <= intervals[j].Start)
                {
                    break;
                }

                if (Overlaps(intervals[i], intervals[j]))
                {
                    AddHardPenalty(result, "hard_overlapping_shifts", 1, OverlapPenalty);
                }
            }
        }
    }

    private static IReadOnlyList<ShiftInterval> MergeOverlappingIntervals(IReadOnlyList<ShiftInterval> intervals)
    {
        if (intervals.Count == 0)
        {
            return Array.Empty<ShiftInterval>();
        }

        var blocks = new List<ShiftInterval>();
        var current = intervals[0];

        for (var i = 1; i < intervals.Count; i++)
        {
            var next = intervals[i];
            if (next.Start < current.End)
            {
                current = new ShiftInterval(
                    current.ShiftId,
                    current.Start,
                    next.End > current.End ? next.End : current.End);
                continue;
            }

            blocks.Add(current);
            current = next;
        }

        blocks.Add(current);
        return blocks;
    }

    private static void AddMinRestPenalties(FitnessResult result, IReadOnlyList<ShiftInterval> workingBlocks, double minRestHours)
    {
        if (minRestHours <= 0)
        {
            return;
        }

        for (var i = 1; i < workingBlocks.Count; i++)
        {
            var restHours = CalculateRestHours(workingBlocks[i - 1], workingBlocks[i]);
            if (restHours >= 0 && restHours < minRestHours)
            {
                AddHardPenalty(
                    result,
                    "hard_min_rest_violation",
                    1,
                    (minRestHours - restHours) * MinRestPenaltyPerHour);
            }
        }
    }

    private static string CreateStableTieBreaker(Chromosome chromosome)
    {
        return string.Join(
            "|",
            GetNormalizedGenes(chromosome).Select(gene =>
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{gene.StartDate.Ticks}:{gene.ShiftId}:{string.Join(",", gene.AssignedEmployeeIds.OrderBy(id => id, StringComparer.Ordinal))}")));
    }

    private static IReadOnlyList<Gene> GetNormalizedGenes(Chromosome chromosome)
    {
        return chromosome.Genes
            .OrderBy(g => g.StartDate)
            .ThenBy(g => g.ShiftId)
            .ToList();
    }

    private static void ShufflePrefix<T>(IList<T> values, int count, Random random)
    {
        for (var i = 0; i < count; i++)
        {
            var selectedIndex = random.Next(i, values.Count);
            (values[i], values[selectedIndex]) = (values[selectedIndex], values[i]);
        }
    }

    private static double GetFinitePositiveHours(ShiftInterval interval)
    {
        var hours = (interval.End - interval.Start).TotalHours;
        return double.IsNaN(hours) || double.IsInfinity(hours) || hours <= 0 ? 0 : hours;
    }

    private static void AddHardPenalty(FitnessResult result, string key, int violations, double penalty)
    {
        if (violations <= 0 || penalty <= 0 || double.IsNaN(penalty) || double.IsInfinity(penalty))
        {
            return;
        }

        result.HardViolationsCount += violations;
        result.HardPenalty += penalty;
        result.PenaltyBreakdown[key] = result.PenaltyBreakdown.GetValueOrDefault(key) + penalty;
    }

    private static void AddSoftPenalty(FitnessResult result, string key, int violations, double penalty)
    {
        if (violations <= 0 || penalty <= 0 || double.IsNaN(penalty) || double.IsInfinity(penalty))
        {
            return;
        }

        result.SoftViolationsCount += violations;
        result.SoftPenalty += penalty;
        result.PenaltyBreakdown[key] = result.PenaltyBreakdown.GetValueOrDefault(key) + penalty;
    }

    private static void EnsureFinite(FitnessResult result)
    {
        if (double.IsNaN(result.HardPenalty) || double.IsInfinity(result.HardPenalty))
        {
            result.HardPenalty = double.MaxValue;
        }

        if (double.IsNaN(result.SoftPenalty) || double.IsInfinity(result.SoftPenalty))
        {
            result.SoftPenalty = double.MaxValue;
        }

        foreach (var key in result.PenaltyBreakdown.Keys.ToList())
        {
            var value = result.PenaltyBreakdown[key];
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                result.PenaltyBreakdown[key] = double.MaxValue;
            }
        }
    }

    private readonly record struct GaOptions(
        int PopulationSize,
        int TournamentSize,
        int ElitismCount,
        int Generations,
        double MutationRate,
        double CrossoverRate,
        int Seed,
        double MinRestHours);

    private static GaOptions ReadOptions(Dictionary<string, object>? options)
    {
        var populationSize = ReadIntOption(options, "populationSize", 50, minValue: 1);
        var defaultElitismCount = Math.Min(1, populationSize);

        return new GaOptions(
            PopulationSize: populationSize,
            TournamentSize: ReadIntOption(options, "tournamentSize", 3, minValue: 1),
            ElitismCount: ReadIntOption(options, "elitismCount", defaultElitismCount, minValue: 0, maxValue: populationSize),
            Generations: ReadIntOption(options, "generations", 100, minValue: 0),
            MutationRate: ReadDoubleOption(options, "mutationRate", 0.1, minValue: 0.0, maxValue: 1.0),
            CrossoverRate: ReadDoubleOption(options, "crossoverRate", 0.8, minValue: 0.0, maxValue: 1.0),
            Seed: ReadIntOption(options, "seed", 12345),
            MinRestHours: ReadDoubleOption(options, "minRestHours", 8.0, minValue: 0.0));
    }

    private static void ValidateInputCollections(SolverInput input)
    {
        if (input.Shifts == null)
        {
            throw new ArgumentException("SolverInput.Shifts cannot be null.", nameof(input));
        }

        if (input.Employees == null)
        {
            throw new ArgumentException("SolverInput.Employees cannot be null.", nameof(input));
        }

        for (var index = 0; index < input.Shifts.Count; index++)
        {
            if (input.Shifts[index] == null)
            {
                throw new ArgumentException(
                    $"SolverInput.Shifts contains a null element at index {index}.",
                    nameof(input));
            }
        }

        for (var index = 0; index < input.Employees.Count; index++)
        {
            if (input.Employees[index] == null)
            {
                throw new ArgumentException(
                    $"SolverInput.Employees contains a null element at index {index}.",
                    nameof(input));
            }
        }
    }

    private static List<Shift> NormalizeShifts(IEnumerable<Shift>? shifts)
    {
        return (shifts ?? Enumerable.Empty<Shift>())
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.Id)
            .ToList();
    }

    private static List<Employee> NormalizeEmployees(IEnumerable<Employee>? employees)
    {
        return (employees ?? Enumerable.Empty<Employee>())
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsEligibleForShift(Employee employee, Shift shift)
    {
        return employee.Assignments?.Any(assignment =>
            assignment != null &&
            string.Equals(assignment.CompanyId, shift.CompanyId, StringComparison.Ordinal) &&
            string.Equals(assignment.DepartmentId, shift.DepartmentId, StringComparison.Ordinal) &&
            string.Equals(assignment.PositionId, shift.PositionId, StringComparison.Ordinal)) == true;
    }

    private static void ValidateUniqueIds(
        IReadOnlyList<Shift> normalizedShifts,
        IReadOnlyList<Employee> normalizedEmployees)
    {
        var employeeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var employee in normalizedEmployees)
        {
            if (!employeeIds.Add(employee.Id))
            {
                throw new ArgumentException($"Duplicate Employee.Id: '{employee.Id}'.", nameof(normalizedEmployees));
            }
        }

        var shiftIds = new HashSet<int>();
        foreach (var shift in normalizedShifts)
        {
            if (!shiftIds.Add(shift.Id))
            {
                throw new ArgumentException($"Duplicate Shift.Id: '{shift.Id}'.", nameof(normalizedShifts));
            }
        }
    }

    private static ShiftInterval NormalizeInterval(Shift shift)
    {
        var start = shift.StartDate;
        var end = shift.EndDate;

        if (end <= start)
        {
            if (end.Ticks > DateTime.MaxValue.Ticks - TimeSpan.TicksPerDay)
            {
                throw new ArgumentException(
                    $"Shift.Id '{shift.Id}' has an invalid interval: StartDate '{shift.StartDate:O}', " +
                    $"EndDate '{shift.EndDate:O}' cannot be advanced by one day.",
                    nameof(shift));
            }

            end = end.AddDays(1);

            if (end <= start)
            {
                throw new ArgumentException(
                    $"Shift.Id '{shift.Id}' has an invalid interval: StartDate '{shift.StartDate:O}', " +
                    $"EndDate '{shift.EndDate:O}' remains non-positive after advancing EndDate by one day.",
                    nameof(shift));
            }
        }

        return new ShiftInterval(shift.Id, start, end);
    }

    private static bool Overlaps(ShiftInterval first, ShiftInterval second)
    {
        return first.Start < second.End && second.Start < first.End;
    }

    private static double CalculateRestHours(ShiftInterval previous, ShiftInterval next)
    {
        return (next.Start - previous.End).TotalHours;
    }

    private static DateTime GetWeekStartMonday(DateTime value)
    {
        var date = value.Date;
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    private static Dictionary<DateTime, double> SplitHoursByWeek(ShiftInterval interval)
    {
        var hoursByWeek = new Dictionary<DateTime, double>();
        var current = interval.Start;

        while (current < interval.End)
        {
            var weekStart = GetWeekStartMonday(current);
            var segmentEnd = interval.End;

            if (weekStart.Ticks <= DateTime.MaxValue.Ticks - (7 * TimeSpan.TicksPerDay))
            {
                var nextWeekStart = weekStart.AddDays(7);
                if (nextWeekStart < segmentEnd)
                {
                    segmentEnd = nextWeekStart;
                }
            }

            if (segmentEnd <= current)
            {
                throw new InvalidOperationException(
                    $"Cannot split Shift.Id '{interval.ShiftId}' into weeks because time cannot advance " +
                    $"from '{current:O}' toward '{interval.End:O}'.");
            }

            var hours = (segmentEnd - current).TotalHours;
            if (!double.IsNaN(hours) && !double.IsInfinity(hours) && hours > 0)
            {
                hoursByWeek[weekStart] = hoursByWeek.GetValueOrDefault(weekStart) + hours;
            }

            current = segmentEnd;
        }

        return hoursByWeek;
    }

    private static int ReadIntOption(
        Dictionary<string, object>? options,
        string key,
        int defaultValue,
        int? minValue = null,
        int? maxValue = null)
    {
        if (!TryGetFiniteDouble(options, key, out var value))
        {
            return defaultValue;
        }

        if (value < int.MinValue || value > int.MaxValue || value != Math.Truncate(value))
        {
            return defaultValue;
        }

        var parsed = (int)value;
        if (minValue.HasValue && parsed < minValue.Value)
        {
            return defaultValue;
        }

        if (maxValue.HasValue && parsed > maxValue.Value)
        {
            return defaultValue;
        }

        return parsed;
    }

    private static double ReadDoubleOption(
        Dictionary<string, object>? options,
        string key,
        double defaultValue,
        double? minValue = null,
        double? maxValue = null)
    {
        if (!TryGetFiniteDouble(options, key, out var parsed))
        {
            return defaultValue;
        }

        if (minValue.HasValue && parsed < minValue.Value)
        {
            return defaultValue;
        }

        if (maxValue.HasValue && parsed > maxValue.Value)
        {
            return defaultValue;
        }

        return parsed;
    }

    private static bool TryGetFiniteDouble(Dictionary<string, object>? options, string key, out double value)
    {
        value = default;
        if (options == null || !options.TryGetValue(key, out var raw))
        {
            return false;
        }

        if (!TryConvertToDouble(raw, out value))
        {
            return false;
        }

        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool TryConvertToDouble(object? raw, out double value)
    {
        value = default;

        switch (raw)
        {
            case null:
                return false;
            case JsonElement json:
                return TryConvertJsonElementToDouble(json, out value);
            case string text:
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            default:
                return false;
        }
    }

    private static bool TryConvertJsonElementToDouble(JsonElement json, out double value)
    {
        value = default;

        return json.ValueKind switch
        {
            JsonValueKind.Number => json.TryGetDouble(out value),
            JsonValueKind.String => double.TryParse(json.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value),
            _ => false
        };
    }
}
