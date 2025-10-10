using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;

namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HideColumns("Error", "StdDev")]
// Consolidated high-board unique-mode profiler:
// Dimensions:
//  - BoardSize: large boards to stress core search
//  - CountOnly: toggle BitmaskSolver.UseCountOnlyUniqueMode (pure counting vs materializing sample solutions)
//  - WithEvents: include/exclude event handler overhead
// Returns: total unique solutions count (uniform return type for both modes)
public class UniqueHighBoardBenchmarkProfiler
{
    // Only benchmark N=14, 16, 18 to avoid excessive profiler runtime
    [Params(14, 16, 18)]
    public int BoardSize;

    // false => materialize sample solutions; true => pure count-only unique mode
    [Params(false, true)]
    public bool CountOnly;

    // false => solver.EnableEvents = false; true => attach lightweight handlers
    [Params(false, true)]
    public bool WithEvents;

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

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

    [Benchmark(Baseline = true)]
    public ulong SolveUnique()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            UseCountOnlyUniqueMode = CountOnly,
            EnableEvents = WithEvents
        };

        if (WithEvents)
        {
            solver.QueenPlaced += (_, _) => _queenPlacedCount++;
            solver.SolutionFound += (_, _) => _solutionsFound++;
            solver.ProgressValueChanged += (_, e) => _lastProgress = e.Value;
        }

        var results = solver.Solve();

        // Guards so JIT cannot elide side effects.
        if (_queenPlacedCount < -1 || _solutionsFound < -1 || _lastProgress < -1)
            throw new InvalidOperationException();

        return results.SolutionsCount; // uniform metric across modes
    }
}
