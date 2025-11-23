namespace NQueen.Benchmarking;

// Consolidated benchmark suite replacing multiple prior specialized benchmark classes.
// Focus areas:
//  - Unique mode: materialize (sample) vs count-only across representative board sizes
//  - All mode: sequential vs parallel, materialize vs count-only, optional prefix/reflection pruning & half-board restriction
// Params chosen to cover scaling inflection points without excessive duplication.

[MemoryDiagnoser]
[CPUUsageDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HideColumns("Error","StdDev")]
public class UniqueModeVariantsBenchmark
{
    // Representative sizes: 12 (small canonical), 14 (mid), 16 (larger), 18 (upper mid)
    [Params(12, 14, 16, 18)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only")]
    public ulong Unique_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = true,
            UseParallel = true
        };
        return solver.Solve().SolutionsCount;
    }

    [Benchmark(Description = "Unique Materialize (capped)")]
    public (int materialized, ulong total) Unique_Materialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = false,
            UseParallel = true
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }
}

[MemoryDiagnoser]
[CPUUsageDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HideColumns("Error","StdDev")]
public class AllModeVariantsBenchmark
{
    // Sizes: 12 (smaller), 14 (mid), 16 (above symmetry throttle threshold)
    [Params(12, 14, 16)]
    public int BoardSize { get; set; }

    // Parallel root split search depth (0 => auto / minimal)
    [Params(0, 2, 4)]
    public int SplitDepth { get; set; }

    // Toggle prefix+reflection pruning pair (covers pruning effectiveness)
    [Params(false, true)]
    public bool EnablePrefixReflection { get; set; }

    // Half-board restriction only meaningful for larger boards (logic inside method clamps)
    [Params(false, true)]
    public bool EnableHalfBoardRestriction { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true, Description = "All Sequential Count-Only")]
    public ulong All_Sequential_CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = EnablePrefixReflection,
            EnablePartialReflectionPruning = EnablePrefixReflection,
            EnableHalfBoardRestriction = EnableHalfBoardRestriction && BoardSize >= 15
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
            EnablePrefixMinimalityPruning = EnablePrefixReflection,
            EnablePartialReflectionPruning = EnablePrefixReflection,
            EnableHalfBoardRestriction = EnableHalfBoardRestriction && BoardSize >= 15
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
            EnablePrefixMinimalityPruning = EnablePrefixReflection,
            EnablePartialReflectionPruning = EnablePrefixReflection,
            EnableHalfBoardRestriction = EnableHalfBoardRestriction && BoardSize >= 15
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
            EnablePrefixMinimalityPruning = EnablePrefixReflection,
            EnablePartialReflectionPruning = EnablePrefixReflection,
            EnableHalfBoardRestriction = EnableHalfBoardRestriction && BoardSize >= 15
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }
}
