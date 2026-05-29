namespace NQueen.Benchmarking;

public class UniqueModeVariantsBenchmark
{
    // Extended range to cover the full CountUniqueFastHalfBoard region (N=16..20)
    [Params(16, 17, 18, 19, 20)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new SolutionFormatter();

    [Benchmark(Baseline = true, Description = "Unique Count-Only (parallel)")]
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

    [Benchmark(Description = "Unique Materialize (sample, capped)")]
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

// Consolidated benchmark suite for All mode variants.
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

    private readonly ISolutionFormatter _formatter = new SolutionFormatter();

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
