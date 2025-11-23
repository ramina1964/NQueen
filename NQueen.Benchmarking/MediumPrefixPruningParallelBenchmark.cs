using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Enums;
using NQueen.Domain.Models;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;

[CPUUsageDiagnoser]
public class MediumPrefixPruningParallelBenchmark
{
    [Params(10, 11, 12, 13)]
    public int BoardSize { get; set; }

    [Params(0, 2, 4)]
    public int SplitDepth { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    [Benchmark(Baseline = true)]
    public ulong CountOnly_All_Parallel_Baseline()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 0)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            EnableIncrementalCanonicalization = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }

    [Benchmark]
    public ulong CountOnly_All_Parallel_WithPrefixReflection()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: 0)
        {
            EnableEvents = false,
            UseParallel = true,
            ParallelRootSplitDepth = SplitDepth,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            EnableIncrementalCanonicalization = false
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}