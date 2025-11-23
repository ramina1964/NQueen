using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NQueen.Domain.Enums;
using NQueen.Domain.Models;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Settings;

namespace NQueen.Benchmarking;

// Benchmark focused on N = 18 Unique mode, comparing Materialize vs CountOnly paths.
// Run example:
//   dotnet run -c Release --project NQueen.Benchmarking -- --filter *UniqueN18Benchmark*
// Columns show allocation and time differences for current engine implementation.
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HideColumns("Error", "StdDev")]
public class UniqueN18Benchmark
{
    private const int BoardSize = 18;
    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true, Description = "Unique CountOnly N=18")]
    public ulong UniqueCountOnly()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = true,
            UseParallel = true
        };
        var results = solver.Solve();
        return results.SolutionsCount;
    }

    [Benchmark(Description = "Unique Materialize N=18 (Cap = MaxDisplayedCount)")]
    public (int materialized, ulong total) UniqueMaterialize()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = false,
            UseParallel = true
        };
        var results = solver.Solve();
        return (results.Solutions.Count, results.SolutionsCount);
    }
}
