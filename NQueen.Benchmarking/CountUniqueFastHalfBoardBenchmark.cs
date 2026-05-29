namespace NQueen.Benchmarking;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[CPUUsageDiagnoser]
public class CountUniqueFastHalfBoardBenchmark
{
    [Params(15, 16, 17)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new NoopFormatter();
    [Benchmark(Baseline = true, Description = "Unique Count-Only Fast Half-Board (N=15-17)")]
    public ulong Unique_CountOnly_N15_17()
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
        solver.SetSimulationToken(Guid.NewGuid());
        return solver.Solve().SolutionsCount;
    }
    }
