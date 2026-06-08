namespace NQueen.Benchmarking;

/// <summary>
/// Measures the performance impact of the solver configuration improvements applied to
/// NQueen.Console in commit 16a43a8: EnablePrefixMinimalityPruning, EnablePartialReflectionPruning,
/// and UseAdaptiveDepth, compared to the old Console-style defaults (all disabled).
///
/// Two modes are benchmarked (All and Unique) across N = 12, 14, 16 so the gains can
/// be observed at small, medium, and large board sizes.
///
/// Run in Release mode. Expected output: the "Console (old)" baseline will be
/// noticeably slower for N >= 14; the gap should widen as N increases.
/// </summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[CPUUsageDiagnoser]
[ShortRunJob]
[WarmupCount(2)]
[IterationCount(5)]
public class ConsolePruningImpactAllBenchmark
{
    // N=12: small — pruning has modest effect.
    // N=14: medium — adaptive depth and pruning both kick in.
    // N=16: large — all three optimisations combine.
    [Params(12, 14, 16)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    /// <summary>Old Console configuration: no pruning, no adaptive depth, events on.</summary>
    [Benchmark(Baseline = true, Description = "All — Console (old): no pruning, events on")]
    public ulong All_Console_Old()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = true,
            UseParallel = true,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableHalfBoardRestriction = false,
            UseAdaptiveDepth = false,
        };
        return solver.Solve().SolutionsCount;
    }

    /// <summary>New Console configuration: pruning on, events off, adaptive depth.</summary>
    [Benchmark(Description = "All — Console (new): pruning on, events off, adaptive depth")]
    public ulong All_Console_New()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            EnableHalfBoardRestriction = BoardSize >= 15,
            UseAdaptiveDepth = BoardSize >= 14,
        };
        return solver.Solve().SolutionsCount;
    }
}

/// <summary>Same comparison for Unique mode.</summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[CPUUsageDiagnoser]
[ShortRunJob]
[WarmupCount(2)]
[IterationCount(5)]
public class ConsolePruningImpactUniqueBenchmark
{
    [Params(12, 14, 16)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();

    /// <summary>Old Console configuration: no pruning, no adaptive depth, events on.</summary>
    [Benchmark(Baseline = true, Description = "Unique — Console (old): no pruning, events on")]
    public ulong Unique_Console_Old()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = true,
            UseParallel = true,
            UseCountOnlyUniqueMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            UseAdaptiveDepth = false,
        };
        return solver.Solve().SolutionsCount;
    }

    /// <summary>New Console configuration: pruning on, events off, adaptive depth.</summary>
    [Benchmark(Description = "Unique — Console (new): pruning on, events off, adaptive depth")]
    public ulong Unique_Console_New()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyUniqueMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            UseAdaptiveDepth = BoardSize >= 14,
        };
        return solver.Solve().SolutionsCount;
    }
}
