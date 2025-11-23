using BenchmarkDotNet.Attributes;
using NQueen.Domain.Enums;
using NQueen.Domain.Models;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Settings;

namespace NQueen.Benchmarking;
public class AllAdaptiveCountOnlyBenchmark
{
    // Representative sizes hitting adaptive path and partial state generation.
    [Params(14, 16)]
    public int BoardSize { get; set; }

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
    [Benchmark(Baseline = true)]
    public ulong CountOnly_All_Adaptive_Parallel()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseParallel = true,
            UseCountOnlyAllMode = true,
            EnableHalfBoardRestriction = false,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false
        };
        return solver.Solve().SolutionsCount;
    }
}