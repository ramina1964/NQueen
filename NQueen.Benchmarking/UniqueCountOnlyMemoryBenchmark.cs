namespace NQueen.Benchmarking;

// Focused memory benchmark: N = 18, Unique mode, Count-Only strategy.
// Measures allocations of the pure counting path (no materialized sample solutions, no events).
// Returns the total unique solutions count (ulong) so BenchmarkDotNet cannot elide work.
[MemoryDiagnoser]
public class UniqueCountOnlyMemoryBenchmark
{
    // Fixed board size per request (N = 18).
    private const int BoardSize = 18;

    private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark(Baseline = true)]
    public ulong CountOnly_Unique_N18()
    {
        using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = true, // activate count-only path
            UseParallel = true // keep parallel on for realistic performance; does not affect materially stored memory
        };

        var results = solver.Solve();
        return results.SolutionsCount; // total unique solutions (not materialized list)
    }
}
