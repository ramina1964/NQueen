using System.Diagnostics;
using NQueen.Domain.Enums;
using NQueen.Domain.Formatters;
using NQueen.Kernel.Solvers;

namespace NQueen.Benchmarking;

internal class Program
{
    static void Main(string[] args)
    {
        // Manual timing harness replacing BenchmarkDotNet due to diagnostics agent issues.
        int boardSize = 16;
        var formatter = new DefaultSolutionFormatter();
        var solver = new BitmaskSolver(boardSize, SolutionMode.Unique, DisplayMode.Hide, formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = true
        };
        // Warmup
        solver.Solve();
        var sw = Stopwatch.StartNew();
        var results = solver.Solve();
        sw.Stop();
        Console.WriteLine($"Unique Count-Only Packed Benchmark (manual) N={boardSize}");
        Console.WriteLine($"SolutionsCount: {results.SolutionsCount}");
        Console.WriteLine($"ElapsedMs: {sw.Elapsed.TotalMilliseconds:F2}");
    }
}
