namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class AllSolutionsEnumerationBenchmark
{
    [Params(12, 13, 14)]
    public int BoardSize { get; set; }

    [Params(0, 2, 4)]
    public int SplitDepth { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark]
    public ulong AllSolutions_Sequential()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public ulong AllSolutions_Parallel()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}

[MemoryDiagnoser]
public class UniqueVsAllMemoryBenchmark
{
    [Params(12, 13)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark]
    public ulong UniqueCountOnly()
    {
        using var solver = new BitmaskSolver(
            BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            UseCountOnlyUniqueMode = true,
            EnableEvents = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public ulong AllSolutions_Materialized()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}
