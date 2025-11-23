namespace NQueen.Benchmarking;

[CPUUsageDiagnoser]
public class BitmaskSolverMicroBenchmark
{
    [Params(12, 14)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    [Benchmark(Baseline = true)]
    public ulong CountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyAllMode = true,
            UseParallel = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public ulong MaterializeSolutions()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyAllMode = false,
            UseParallel = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}