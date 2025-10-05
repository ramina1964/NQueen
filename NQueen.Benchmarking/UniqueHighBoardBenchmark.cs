using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;

namespace NQueen.Benchmarking;

// Targeted benchmark for Unique mode performance (materializing vs count-only)
// Now runs for N=14 and N=16 for scaling comparison.
// Example outside VS:
//   dotnet run -c Release -p NQueen.Benchmarking -- --filter *UniqueHighBoardBenchmark.BitmaskUniqueCountOnly* --maxIterationCount 10
//   dotnet run -c Release -p NQueen.Benchmarking -- --filter *UniqueHighBoardBenchmark* 
// Use --join to get combined summary. Adjust max iteration counts if runs are lengthy.

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HideColumns("Error", "StdDev")] // shorten table
public class UniqueHighBoardBenchmark
{
    // N=14 and N=16 for focused profiling and scaling analysis.
    [Params(14, 16)]
    public int BoardSize;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    // Baseline: full Unique mode materializing (capped internally if cap enabled)
    [Benchmark(Baseline = true)]
    public SimulationResults BitmaskUniqueMaterialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        solver.UseCountOnlyUniqueMode = false; // ensure materialization path
        return solver.Solve();
    }

    // Optimized: count-only Unique mode (no solution arrays retained)
    [Benchmark]
    public SimulationResults BitmaskUniqueCountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter);
        solver.UseCountOnlyUniqueMode = true; // triggers count-only path
        return solver.Solve();
    }
}
