using BenchmarkDotNet.Diagnosers;
using Microsoft.VSDiagnostics;
using NQueen.Kernel;

namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[CPUUsageDiagnoser]
public class NQueenBench
{
    [Params(20)]
    public int N { get; set; }

    [Benchmark]
    public long CountOnly()
    {
        return BitboardNQueenSolver.CountSolutions(N, parallel: true);
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        var benchMode = Environment.GetEnvironmentVariable("BENCHMARK_MODE");
        if ((benchMode == "1") || (args != null && args.Any(a => a.StartsWith("--"))))
        {
            // Run all benchmarks in this assembly when invoked by BenchmarkDotNet infrastructure.
            BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            return;
        }

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

        // Warmup
        solverCount.Solve();

        // Force a stable GC baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocBefore = GC.GetTotalAllocatedBytes(true);
        long gen0Before = GC.CollectionCount(0);
        long gen1Before = GC.CollectionCount(1);
        long gen2Before = GC.CollectionCount(2);
        long wsBefore = Process.GetCurrentProcess().WorkingSet64;

        var sw = Stopwatch.StartNew();
        var res = solverCount.Solve();
        sw.Stop();

        long allocAfter = GC.GetTotalAllocatedBytes(true);
        long gen0After = GC.CollectionCount(0);
        long gen1After = GC.CollectionCount(1);
        long gen2After = GC.CollectionCount(2);
        long wsAfter = Process.GetCurrentProcess().WorkingSet64;

        Console.WriteLine($"All Mode Count-Only N={n}");
        Console.WriteLine($"SolutionsCount: {res.SolutionsCount}");
        Console.WriteLine($"ElapsedMs: {sw.Elapsed.TotalMilliseconds:F2}");
        Console.WriteLine($"AllocatedBytes (process): {allocAfter - allocBefore:N0}");
        Console.WriteLine($"GC Gen0/1/2 Collections: {gen0After - gen0Before}/{gen1After - gen1Before}/{gen2After - gen2Before}");
        Console.WriteLine($"WorkingSet Delta (bytes): {wsAfter - wsBefore:N0}");
    }
}
