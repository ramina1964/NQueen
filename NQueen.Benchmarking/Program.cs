using System;
using System.Diagnostics;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Enums;
using NQueen.Domain.Formatters;

internal class Program
{
    static void Main(string[] args)
    {
        int n = 19; // High-N run for All count-only
        var formatter = new DefaultSolutionFormatter();
        using var solverCount = new BitmaskSolver(n, SolutionMode.All, DisplayMode.Hide, formatter)
        {
            EnableEvents = false,
            UseCountOnlyAllMode = true,
            EnablePrefixMinimalityPruning = false,
            EnablePartialReflectionPruning = false,
            UseParallel = true,
            ParallelRootSplitDepth = n >= 16 ? 3 : 1
        };
        solverCount.Solve(); // warmup
        var sw = Stopwatch.StartNew();
        var res = solverCount.Solve();
        sw.Stop();
        Console.WriteLine($"All Mode Count-Only N={n}");
        Console.WriteLine($"SolutionsCount: {res.SolutionsCount}");
        Console.WriteLine($"ElapsedMs: {sw.Elapsed.TotalMilliseconds:F2}");
    }
}
