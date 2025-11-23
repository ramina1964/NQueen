namespace NQueen.Benchmarking;

[CPUUsageDiagnoser]
public class HalfBoardRestrictionBenchmark
{
    // Force enumeration path (below lookup threshold 20)
    [Params(15, 16, 17, 18, 19)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    [Benchmark(Baseline = true)]
    public ulong CountOnly_All_NoHalfBoard()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 0)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = true,
            EnableHalfBoardRestriction = false,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableIncrementalCanonicalization = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public ulong CountOnly_All_WithHalfBoard()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 0)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = true,
            EnableHalfBoardRestriction = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableIncrementalCanonicalization = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public SimulationResults Materialize_All_WithHalfBoard()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 5)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = false,
            EnableHalfBoardRestriction = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableIncrementalCanonicalization = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results;
    }
}