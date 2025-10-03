    namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class BitmaskSolverParallelizationBaselineBenchmark
{
    [Params(10, 12, 14)]
    public int BoardSize;

    [Params(SolutionMode.All, SolutionMode.Unique, SolutionMode.Single)]
    public SolutionMode SolutionMode;

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true)]
    public SimulationResults SolveCurrent()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode, DisplayMode.Hide, _formatter)
        {
            DelayInMillisec = 0,
            EnableEvents = false
        };
        return solver.Solve();
    }
}