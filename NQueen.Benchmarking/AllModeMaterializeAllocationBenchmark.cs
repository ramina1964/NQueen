using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;
[SimpleJob(warmupCount: 3, iterationCount: 15)]
[CPUUsageDiagnoser]
public class AllModeMaterializeAllocationBenchmark
{
    [Params(15, 18)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();
    [Benchmark(Description = "All Parallel Materialize Allocations (N=15,18)")]
    public (int materialized, ulong total) All_Parallel_Materialize_Allocations()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyAllMode = false,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableHalfBoardRestriction = BoardSize >= 15,
            ParallelRootSplitDepth = 3,
            UseAdaptiveDepth = true
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }
}