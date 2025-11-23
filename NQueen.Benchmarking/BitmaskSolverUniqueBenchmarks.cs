namespace NQueen.Benchmarking;

public class BitmaskSolverUniqueBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        _formatter = new SolutionFormatter();
        _boardSize = 16;
    }

    [Benchmark]
    public SimulationResults SolveUnique()
    {
        var solver = new BitmaskSolver(_boardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        return solver.Solve();
    }

    private SolutionFormatter _formatter = null!;
    private int _boardSize;
}