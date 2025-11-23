namespace NQueen.Benchmarking;

public class UniqueCountOnlyLightweightBenchmark
{
    [Params(14, 15, 16)]
    public int BoardSize { get; set; }

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true)]
    public ulong CountOnly_Unique()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            UseCountOnlyUniqueMode = true,
            EnableEvents = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}
