namespace NQueen.Benchmarking;

/// <summary>
/// Benchmarks for Unique mode count-only paths across a range of board sizes.
/// Covers packed-set counting (N=16), fast half-board (N=15-17),
/// the high-N region (N=16-20), and the extended half-board region (N=18-22).
/// </summary>

/// <summary>Packed-set unique counter at N=16 (baseline single-size check).</summary>
public class UniqueCountPackedBenchmark
{
    [Params(16)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark]
    public ulong CountUniquePacked()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = true
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}

/// <summary>Fast half-board unique count-only path for N=15-17.</summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[CPUUsageDiagnoser]
public class UniqueFastHalfBoardBenchmark
{
    [Params(15, 16, 17)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only Fast Half-Board (N=15-17)")]
    public ulong Unique_CountOnly_HalfBoard()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyUniqueMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            EnableHalfBoardRestriction = BoardSize >= 15
        };
        return solver.Solve().SolutionsCount;
    }
}

/// <summary>Count-only vs materialise comparison across high-N boards (N=16-20).</summary>
[CPUUsageDiagnoser]
[ShortRunJob]
[WarmupCount(1)]
[IterationCount(3)]
public class UniqueHighNBenchmark
{
    [Params(16, 17, 18, 19, 20)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only")]
    public ulong Unique_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyUniqueMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            EnableHalfBoardRestriction = BoardSize >= 15,
            ParallelRootSplitDepth = 3,
            UseAdaptiveDepth = true
        };
        return solver.Solve().SolutionsCount;
    }

    [Benchmark(Description = "Unique Materialize (capped)")]
    public (int materialized, ulong total) Unique_Materialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyUniqueMode = false,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            EnableHalfBoardRestriction = BoardSize >= 15,
            ParallelRootSplitDepth = 3,
            UseAdaptiveDepth = true
        };
        var r = solver.Solve();
        return (r.Solutions.Count, r.SolutionsCount);
    }
}

/// <summary>Extended half-board region (N=18-22), memory-focused.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class UniqueHalfBoardHighNBenchmark
{
    [Params(18, 19, 20, 21, 22)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only (parallel) half-board N=18-22")]
    public ulong Unique_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyUniqueMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException("Unexpected zero unique solutions.");
        return results.SolutionsCount;
    }
}
