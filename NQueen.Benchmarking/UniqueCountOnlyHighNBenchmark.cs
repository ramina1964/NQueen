namespace NQueen.Benchmarking;

[CPUUsageDiagnoser]
[ShortRunJob]
[WarmupCount(1)]
[IterationCount(3)]
public class UniqueCountOnlyHighNBenchmark
{
    [Params(16, 17, 18, 19, 20)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

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
        solver.SetSimulationToken(Guid.NewGuid());
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
        solver.SetSimulationToken(Guid.NewGuid());
        var r = solver.Solve();
        return (r.Solutions.Count, r.SolutionsCount);
    }
}
