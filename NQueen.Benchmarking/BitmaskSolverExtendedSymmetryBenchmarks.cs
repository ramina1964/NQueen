namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class BitmaskSolverExtendedSymmetryBenchmarks
{
    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    private readonly BitmaskSolver _solver;

    public BitmaskSolverExtendedSymmetryBenchmarks()
    {
        // disable cap -> pass enableCap:false
        _solver = new BitmaskSolver(_formatter, enableCap: false);
    }

    [Params(8, 10, 12)]
    public int N { get; set; }

    [Benchmark]
    public SimulationResults AllSolutions() => new BitmaskSolver(N, SolutionMode.All, DisplayMode.Hide, _formatter).Solve();

    [Benchmark]
    public SimulationResults UniqueSolutions() => new BitmaskSolver(N, SolutionMode.Unique, DisplayMode.Hide, _formatter).Solve();
}