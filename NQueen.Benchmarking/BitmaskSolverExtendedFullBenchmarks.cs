namespace NQueen.Benchmarking;

// Comprehensive benchmark to help pinpoint GUI vs core solver overhead.
// Dimensions measured:
//  - BoardSize: typical interactive sizes
//  - SolutionMode: All / Unique / Single
//  - DisplayMode: Hide vs Visualize (event & incremental painting cost)
//  - AttachEventHandlers: simulate WPF subscriptions (QueenPlaced / SolutionFound / Progress)
// Use results to compare with ConsoleApp baselines (which effectively run Hide + no handlers).

[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class BitmaskSolverExtendedFullBenchmarks
{
    // Keep sizes moderate so the cartesian space remains tractable.
    [Params(8, 10, 12, 14)]
    public int BoardSize;

    [Params(SolutionMode.All, SolutionMode.Unique, SolutionMode.Single)]
    public SolutionMode SolutionMode;

    [Params(DisplayMode.Hide, DisplayMode.Visualize)]
    public DisplayMode DisplayMode;

    // false = ConsoleApp like scenario; true = approximate GUI subscription overhead.
    [Params(false, true)]
    public bool AttachEventHandlers;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    // Counters so handler work is not optimized away.
    private int _queenPlacedCount;
    private int _solutionsFound;
    private double _lastProgress;

    [GlobalSetup]
    public void Setup()
    {
        _queenPlacedCount = 0;
        _solutionsFound = 0;
        _lastProgress = 0;
    }

    [Benchmark]
    public SimulationResults Solve()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode, DisplayMode, _formatter);

        if (AttachEventHandlers)
        {
            // Lightweight handlers; GUI does more work (dispatcher, allocation) so this is a lower bound.
            solver.QueenPlaced += (_, _) => _queenPlacedCount++;
            solver.SolutionFound += (_, _) => _solutionsFound++;
            solver.ProgressValueChanged += (_, e) => _lastProgress = e.Value;
        }

        var results = solver.Solve();

        // Guard usage so JIT cannot elide counters.
        if (_queenPlacedCount < -1 || _solutionsFound < -1 || _lastProgress < -1)
            throw new InvalidOperationException();

        return results;
    }
}
