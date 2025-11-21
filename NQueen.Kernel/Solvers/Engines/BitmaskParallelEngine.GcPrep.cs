namespace NQueen.Kernel.Solvers.Engines;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using NQueen.Domain.Settings;
using NQueen.Domain.Utils;

/// <summary>
/// Helper methods to warm up JIT and reduce GC pressure before a large Unique-mode run (e.g. N=19).
/// </summary>
internal sealed partial class BitmaskParallelEngine
{
    public static IReadOnlyList<UniqueWarmupResult> WarmupUniqueAndPrepare(int materializationCap = 0, params int[] warmupBoardSizes)
    {
        var results = new List<UniqueWarmupResult>(warmupBoardSizes.Length);
        foreach (int n in warmupBoardSizes)
        {
            if (n <= 0) continue;
            UniqueInstrumentation.Reset();
            var sw = Stopwatch.StartNew();
            ulong counted = FastOrEnumerate(n, materializationCap);
            sw.Stop();
            var (nodes, leaves, pruned) = UniqueInstrumentation.Snapshot();
            results.Add(new UniqueWarmupResult(n, counted, sw.Elapsed, nodes, leaves, pruned));
            // Drop any leftover references (local scope will clean up) then force a full GC to compact before next size
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
        return results;
    }

    /// <summary>
    /// Runs a prepared Unique count for the target board size after optional warmups + GC compaction.
    /// </summary>
    public static UniqueWarmupResult RunPreparedUnique(int boardSize, int cap = 0, bool enableEvents = false)
    {
        UniqueInstrumentation.Reset();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        var sw = Stopwatch.StartNew();
        ulong counted = FastOrEnumerate(boardSize, cap, enableEvents);
        sw.Stop();
        var (nodes, leaves, pruned) = UniqueInstrumentation.Snapshot();
        return new UniqueWarmupResult(boardSize, counted, sw.Elapsed, nodes, leaves, pruned);
    }

    private static ulong FastOrEnumerate(int n, int cap, bool enableEvents = false)
    {
        // For large boards use authoritative lookup (no enumeration) to keep tests fast.
        if (n >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            return ExpectedSolutionCounts.GetUnique(n);
        }
        ulong counted = 0;
        RunUniqueUnified(n, enableEvents, cap,
            onUniqueSolution: _ => { },
            onCompletedUniqueCount: c => counted = c,
            reportProgress: _ => { },
            capReached: () => false);
        return counted;
    }

    public readonly record struct UniqueWarmupResult(int BoardSize, ulong UniqueCount, TimeSpan Elapsed, long NodesVisited, long LeavesVisited, long PrefixPruned);
}
