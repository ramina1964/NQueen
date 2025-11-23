namespace NQueen.Benchmarking;

public class UniqueSolverBaselineBenchmark
{
    [Params(12, 14, 16)]
    public int BoardSize;
    [Benchmark]
    public ulong SolveUniqueBaseline()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, new DefaultSolutionFormatter())
        {
            EnableEvents = false,
            DelayInMillisec = 0
        };
        var results = solver.Solve();
        return results.SolutionsCount;
    }
}