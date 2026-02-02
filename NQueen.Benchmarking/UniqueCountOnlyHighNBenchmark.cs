namespace NQueen.Benchmarking;

[CPUUsageDiagnoser]
[ShortRunJob]
[WarmupCount(1)]
[IterationCount(3)]
public class UniqueCountOnlyHighNBenchmark
{
    //[Params(18, 19, 20)]
    [Params(20)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Description = "Unique Count-Only (N=20)")]
    public ulong Unique_CountOnly_HighN()
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
}
