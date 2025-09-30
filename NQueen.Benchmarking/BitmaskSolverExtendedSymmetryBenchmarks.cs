namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class BitmaskSolverExtendedSymmetryBenchmarks
{
    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    private readonly BitmaskSolverExtended _solver;

    public BitmaskSolverExtendedSymmetryBenchmarks()
    {
        // disable cap -> pass enableCap:false
        _solver = new BitmaskSolverExtended(_formatter, enableCap: false);
    }

    [Params(8, 10, 12)]
    public int N { get; set; }

    [Benchmark]
    public SimulationResults AllSolutions() => new BitmaskSolverExtended(N, SolutionMode.All, DisplayMode.Hide, _formatter).Solve();

    [Benchmark]
    public SimulationResults UniqueSolutions() => new BitmaskSolverExtended(N, SolutionMode.Unique, DisplayMode.Hide, _formatter).Solve();
}