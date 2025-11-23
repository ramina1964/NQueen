namespace NQueen.Kernel.Solvers.Engines;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NQueen.Domain.Utils;

/// <summary>
/// Symmetry-aware unique solution counter for boards below lookup threshold.
/// Enumerates all solutions but only counts those that are canonical minimal under dihedral group.
/// Parallelized by first column choice. Materializes up to <paramref name="cap"/> canonical solutions.
/// NOTE: This still traverses full search space; prefix symmetry pruning can be added later for further optimization.
/// </summary>
public static class SymmetryPrunedUniqueCounter
{
    public static ulong Count(int boardSize, int cap, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0UL;
        int N = boardSize;
        ulong maskAll = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        var totalCount = 0UL;
        var materializedQueue = (onMaterialized != null && cap > 0) ? new ConcurrentQueue<int[]>() : null;

        // Pruning gate (reuse heuristic from BitmaskSearchEngine)
        int pruneDepthGate = int.MaxValue;
        if (SearchOptimizations.PrefixMinimalityPruningEnabled || SearchOptimizations.ReflectionPrefixPruningEnabled)
        {
            if (N >= 20) pruneDepthGate = 1;
            else if (N >= 16) pruneDepthGate = 2;
        }

        // Partition first-column roots to reduce scheduling overhead & allow buffer reuse per worker.
        Parallel.ForEach(System.Collections.Concurrent.Partitioner.Create(0, N), range =>
        {
            int start = range.Item1;
            int end = range.Item2;
            // Reuse buffers per worker.
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            int[] scratch = new int[N * 8];
            int localMaterialized = 0; // per worker (queue trimmed later)
            ulong localCount = 0UL;
            // Optionally accumulate locally then enqueue in batch to reduce contention.
            System.Collections.Generic.List<int[]>? localMaterializedList = (materializedQueue != null) ? new System.Collections.Generic.List<int[]>(cap > 0 ? Math.Min(cap, 128) : 0) : null;

            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    // Canonical minimality test.
                    var canon = SymmetryHelper.GetCanonicalForm(rows, scratch, null);
                    bool minimal = true;
                    for (int i = 0; i < N; i++) { if (rows[i] != canon[i]) { minimal = false; break; } }
                    if (minimal)
                    {
                        localCount++;
                        if (localMaterializedList != null && localMaterialized < cap && onMaterialized != null)
                        {
                            var copy = new int[N];
                            Array.Copy(rows, copy, N);
                            localMaterializedList.Add(copy);
                            localMaterialized++;
                        }
                    }
                    return;
                }
                ulong avail = ~(cols | d1 | d2) & maskAll;
                while (avail != 0)
                {
                    ulong bit = avail & (ulong)-(long)avail;
                    avail ^= bit;
                    int r = System.Numerics.BitOperations.TrailingZeroCount(bit);
                    rows[col] = r;
                    // Early prefix pruning (reflection + minimality) only after gate depth
                    if (col >= pruneDepthGate && ShouldPrunePrefixFast(rows, col, N))
                    { rows[col] = -1; continue; }
                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    rows[col] = -1;
                }
            }

            for (int rootRow = start; rootRow < end; rootRow++)
            {
                rows[0] = rootRow; // only index 0 needs update; deeper indices reset by DFS unwinding
                ulong bit0 = 1UL << rootRow;
                DFS(1, bit0, bit0 << 1, bit0 >> 1);
                // leave rows[0] set; will overwrite next iteration
            }

            System.Threading.Interlocked.Add(ref totalCount, localCount);
            if (localMaterializedList != null)
            {
                foreach (var sol in localMaterializedList)
                    materializedQueue!.Enqueue(sol);
            }
        });

        if (materializedQueue != null && onMaterialized != null)
        {
            int emitted = 0;
            while (emitted < cap && materializedQueue.TryDequeue(out var sol))
            {
                onMaterialized(sol);
                emitted++;
            }
        }
        return totalCount;
    }

    private static bool ShouldPrunePrefixFast(int[] rows, int depth, int N)
    {
        bool reflectionEnabled = SearchOptimizations.ReflectionPrefixPruningEnabled;
        bool minimalityEnabled = SearchOptimizations.PrefixMinimalityPruningEnabled;
        if (!reflectionEnabled && !minimalityEnabled) return false;
        if (reflectionEnabled)
        {
            for (int i = 0; i <= depth; i++)
            {
                int r = rows[i]; if (r < 0) return false; // incomplete prefix
                int reflected = N - 1 - r;
                if (r > reflected) return true; // lexicographically greater than horizontal reflection
                if (r < reflected) break; // strictly smaller, keep
            }
        }
        if (!minimalityEnabled) return false;
        for (int i = 0; i <= depth; i++)
        {
            int a = rows[i]; if (a < 0) return false;
            int b = rows[depth - i]; if (b < 0) return false;
            int transformed = N - 1 - b; // 180 rotation composed with horizontal reflection of reversed prefix
            if (a > transformed) return true;
            if (a < transformed) break;
        }
        return false;
    }
}
