using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NQueen.Kernel.Solvers;

namespace NQueen.Benchmarking;
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.HostProcess, warmupCount:1, iterationCount:5, launchCount:1)]
public class HighBoardAllBenchmarks
{
    // Explicit param source for reliable discovery
    public static IEnumerable<int> BoardSizes() { yield return 18; yield return 19; }

    [ParamsSource(nameof(BoardSizes))]
    public int BoardSize { get; set; }

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    [Benchmark(Baseline = true)]
    public ulong CountOnly_All()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false
        };
        var res = solver.Solve();
        if (res.SolutionsCount == 0)
            throw new InvalidOperationException();
        return res.SolutionsCount;
    }

    [Benchmark]
    public ulong Materialize_All()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyAllMode = false,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false
        };
        var res = solver.Solve();
        if (res.SolutionsCount == 0)
            throw new InvalidOperationException();
        return res.SolutionsCount;
    }
}