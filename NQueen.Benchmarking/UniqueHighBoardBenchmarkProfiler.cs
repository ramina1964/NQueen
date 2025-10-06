using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;

namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HideColumns("Error", "StdDev")]
public class UniqueHighBoardBenchmarkProfiler
{
    // N=16 and N=18 for performance profiling.
    [Params(16, 18)]
    public int BoardSize;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    private int _queenPlacedCount;
    private int _solutionsFound;
    private double _lastProgress;

    [Benchmark(Baseline = true)]
    public SimulationResults BitmaskUniqueMaterialize()
    {
        _queenPlacedCount = 0;
        _solutionsFound = 0;
        _lastProgress = 0;
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        solver.QueenPlaced += (_, _) => _queenPlacedCount++;
        solver.SolutionFound += (_, _) => _solutionsFound++;
        solver.ProgressValueChanged += (_, e) => _lastProgress = e.Value;
        var results = solver.Solve();
        // Guard usage so JIT cannot elide counters.
        if (_queenPlacedCount < -1 || _solutionsFound < -1 || _lastProgress < -1)
            throw new InvalidOperationException();
        return results;
    }

    [Benchmark]
    public SimulationResults BitmaskUniqueCountOnly()
    {
        _queenPlacedCount = 0;
        _solutionsFound = 0;
        _lastProgress = 0;
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        solver.QueenPlaced += (_, _) => _queenPlacedCount++;
        solver.SolutionFound += (_, _) => _solutionsFound++;
        solver.ProgressValueChanged += (_, e) => _lastProgress = e.Value;
        var results = solver.Solve();
        if (_queenPlacedCount < -1 || _solutionsFound < -1 || _lastProgress < -1)
            throw new InvalidOperationException();
        return results;
    }
}
