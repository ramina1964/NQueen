namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class BitmaskSolverExtendedSymmetryBenchmarks
{
    [Params(8, 12, 14, 15)]
    public int BoardSize;

    private BitmaskSolverExtended? _solver;
    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [GlobalSetup]
    public void Setup()
    {
        _solver = new BitmaskSolverExtended(_formatter, disableCap: true);
        // BoardSize, SolutionMode, DisplayMode are set via constructor, not property setters
    }

    [Benchmark]
    public SimulationResults SolveUnique()
    {
        if (_solver == null)
            throw new System.InvalidOperationException();
        // Use the constructor that sets BoardSize, SolutionMode, DisplayMode
        var solver = new BitmaskSolverExtended(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        return solver.Solve();
    }
}