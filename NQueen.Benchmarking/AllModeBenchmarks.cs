namespace NQueen.Benchmarking;

/// <summary>All-mode count-only at N=18 — single-size focused timing.</summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class AllCountOnlyN18Benchmark
{
    [Params(18)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true, Description = "All Parallel Count-Only (N=18)")]
    public ulong All_Parallel_CountOnly_N18()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableHalfBoardRestriction = BoardSize >= 15,
            ParallelRootSplitDepth = 3,
            UseAdaptiveDepth = true
        };
        return solver.Solve().SolutionsCount;
    }
}

/// <summary>
/// All-mode parallel count-only at N=16 and N=18 with the same full job as
/// <see cref="UniqueFastHalfBoardEvenOddBenchmark"/> (3 warmups, 15 measured iterations)
/// so wall-clock deltas land in a comparable confidence interval.
/// <para>
/// Targets the All-mode hot path through <c>BitboardNQueenSolver.CountSolutions</c>'s
/// parallel branch — the same path the <c>perf/all-work-stealing</c> experiment swaps
/// the partitioner on. N=16 catches the size where the half-board restriction first
/// makes the per-item cost-variance hurt; N=18 carries enough total work that any
/// tail-imbalance reduction shows up clearly above noise.
/// </para>
/// </summary>
[SimpleJob(warmupCount: 3, iterationCount: 15)]
public class AllCountOnlyParallelScalingBenchmark
{
    [Params(16, 18)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Description = "All Parallel Count-Only Scaling (N=16,18)")]
    public ulong All_Parallel_CountOnly_Scaling()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableHalfBoardRestriction = BoardSize >= 15,
            ParallelRootSplitDepth = 3,
            UseAdaptiveDepth = true
        };
        return solver.Solve().SolutionsCount;
    }
}

/// <summary>
/// Compares All-mode strategies across split depths and pruning settings.
/// 2 sizes × 2 depths × 2 pruning states × 4 methods = 32 combinations.
/// </summary>
public class AllModeVariantsBenchmark
{
    // Two representative sizes; N=12 is medium, N=15 is where half-board restriction kicks in.
    [Params(12, 15)]
    public int BoardSize { get; set; }

    // SplitDepth 2 (shallow) vs 3 (recommended default).
    [Params(2, 3)]
    public int SplitDepth { get; set; }

    // false = no pruning baseline, true = both prefix + reflection pruning enabled.
    [Params(false, true)]
    public bool EnablePruning { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true, Description = "All Sequential Count-Only")]
    public ulong All_Sequential_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = EnablePruning,
            EnablePartialReflectionPruning = EnablePruning,
            EnableHalfBoardRestriction = EnablePruning && BoardSize >= 15
        };
        return solver.Solve().SolutionsCount;
    }

    [Benchmark(Description = "All Sequential Materialize (capped)")]
    public (int materialized, ulong total) All_Sequential_Materialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = false,
            EnablePrefixMinimalityPruning = EnablePruning,
            EnablePartialReflectionPruning = EnablePruning,
            EnableHalfBoardRestriction = EnablePruning && BoardSize >= 15
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }

    [Benchmark(Description = "All Parallel Count-Only")]
    public ulong All_Parallel_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = EnablePruning,
            EnablePartialReflectionPruning = EnablePruning,
            EnableHalfBoardRestriction = EnablePruning && BoardSize >= 15
        };
        return solver.Solve().SolutionsCount;
    }

    [Benchmark(Description = "All Parallel Materialize (capped)")]
    public (int materialized, ulong total) All_Parallel_Materialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth,
            UseCountOnlyAllMode = false,
            EnablePrefixMinimalityPruning = EnablePruning,
            EnablePartialReflectionPruning = EnablePruning,
            EnableHalfBoardRestriction = EnablePruning && BoardSize >= 15
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }
}

/// <summary>Compares prefix-pruning impact for All mode across medium board sizes.</summary>
public class AllPrefixPruningBenchmark
{
    [Params(10, 11, 12, 13, 14)]
    public int BoardSize { get; set; }

    [Params(0, 2, 4)]
    public int SplitDepth { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true)]
    public ulong CountOnly_Baseline()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 0)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public ulong CountOnly_WithPrefixReflection()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 0)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}
