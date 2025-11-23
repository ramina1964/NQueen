namespace NQueen.Benchmarking;

// Simple manual profiler for N=18 and N=19 Unique mode producing elapsed time & GC stats.
// Overwrites prior profiling outputs when re-run.
public static class UniqueN18Profiler
{
    public record ProfileResult(int BoardSize, bool CountOnly, double ElapsedSec, long Gen0, long Gen1, long Gen2, long AllocBytes, ulong Solutions, int Materialized);

    public static ProfileResult Run(int boardSize, bool countOnly)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long before0 = GC.CollectionCount(0);
        long before1 = GC.CollectionCount(1);
        long before2 = GC.CollectionCount(2);
        long beforeAlloc = GC.GetTotalMemory(forceFullCollection: false);

        var formatter = new DefaultSolutionFormatter();
        using var solver = new BitmaskSolver(boardSize, SolutionMode.Unique, DisplayMode.Hide, formatter)
        {
            EnableEvents = false,
            UseCountOnlyUniqueMode = countOnly,
            UseParallel = true
        };
        var sw = Stopwatch.StartNew();
        var results = solver.Solve();
        sw.Stop();

        long afterAlloc = GC.GetTotalMemory(forceFullCollection: false);
        return new ProfileResult(
            boardSize,
            countOnly,
            Math.Round(sw.Elapsed.TotalSeconds, 1),
            GC.CollectionCount(0) - before0,
            GC.CollectionCount(1) - before1,
            GC.CollectionCount(2) - before2,
            afterAlloc - beforeAlloc,
            results.SolutionsCount,
            results.Solutions.Count);
    }
}
