using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Enums; // adjust if actual enums namespace differs
using NQueen.Domain.Models; // for ISolutionFormatter / DefaultSolutionFormatter
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking;

[CPUUsageDiagnoser]
public class AllModePrefixPruningBenchmark
{
    [Params(14, 15, 16)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    // Baseline: pruning & incremental disabled
    [Benchmark(Baseline = true)]
    public ulong CountOnly_All_Baseline()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false,
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

    // Pruning enabled (prefix+reflection); incremental disabled
    [Benchmark]
    public ulong CountOnly_All_WithPrefixReflection()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false,
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

    // Pruning enabled + incremental canonicalization placeholder enabled
    [Benchmark]
    public ulong CountOnly_All_WithIncremental()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = false,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = true,
            EnablePartialReflectionPruning = true,
            EnableIncrementalCanonicalization = true
        };
        var results = solver.Solve();
        if (results.SolutionsCount == 0)
            throw new InvalidOperationException();
        return results.SolutionsCount;
    }
}