namespace NQueen.Kernel.Solvers.Engines;

using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent; // duplicate harmless
using System.Threading.Tasks;
using NQueen.Domain.Utils;

public static class SymmetryPrunedUniqueCounter
{
    public static ulong Count(int boardSize, int cap, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0UL;
        int N = boardSize;
        ulong maskAll = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        var totalCount = 0UL;
        var materializedQueue = (onMaterialized != null && cap > 0) ? new ConcurrentQueue<int[]>() : null;
        int pruneDepthGate = int.MaxValue;
        if (SearchOptimizations.PrefixMinimalityPruningEnabled || SearchOptimizations.ReflectionPrefixPruningEnabled)
        {
            if (N >= 20) pruneDepthGate = 1; else if (N >= 16) pruneDepthGate = 2;
        }

        Parallel.ForEach(Partitioner.Create(0, N), range =>
        {
            // Per-worker reusable buffers
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            int[] scratch = new int[N * 8];
            ulong[] stackAvail = new ulong[N];
            ulong[] stackCols  = new ulong[N];
            ulong[] stackD1    = new ulong[N];
            ulong[] stackD2    = new ulong[N];
            int localMaterialized = 0; ulong localCount = 0UL;
            System.Collections.Generic.List<int[]>? localMaterializedList = (materializedQueue != null) ? new System.Collections.Generic.List<int[]>(Math.Min(cap, 256)) : null;

            void EnumerateRoot(int rootRow)
            {
                // Initialize root state
                rows[0] = rootRow;
                ulong bit0 = 1UL << rootRow;
                ulong cols = bit0;
                ulong d1 = bit0 << 1;
                ulong d2 = bit0 >> 1;
                int col = 1;
                ulong avail = ~(cols | d1 | d2) & maskAll;
                while (true)
                {
                    if (col == N)
                    {
                        if (SymmetryHelper.IsIdentityCanonical(rows, scratch))
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
                        col--; // backtrack
                        if (col <= 0) break;
                        avail = stackAvail[col];
                        cols  = stackCols[col];
                        d1    = stackD1[col];
                        d2    = stackD2[col];
                        rows[col] = -1;
                        continue;
                    }
                    if (avail == 0)
                    {
                        col--;
                        if (col <= 0) break;
                        avail = stackAvail[col];
                        cols  = stackCols[col];
                        d1    = stackD1[col];
                        d2    = stackD2[col];
                        rows[col] = -1;
                        continue;
                    }
                    // Extract next bit
                    ulong bit = avail & (ulong)-(long)avail;
                    avail ^= bit;
                    int r = System.Numerics.BitOperations.TrailingZeroCount(bit);
                    rows[col] = r;
                    if (col >= pruneDepthGate && ShouldPrunePrefixFast(rows, col, N))
                    {
                        rows[col] = -1; // skip branch
                        continue;
                    }
                    // Save current state before descending
                    stackAvail[col] = avail;
                    stackCols[col]  = cols;
                    stackD1[col]    = d1;
                    stackD2[col]    = d2;
                    // Advance
                    cols |= bit;
                    d1 = (d1 | bit) << 1;
                    d2 = (d2 | bit) >> 1;
                    col++;
                    if (col < N)
                        avail = ~(cols | d1 | d2) & maskAll;
                }
                rows[0] = -1; // reset root slot for next root
            }

            for (int rootRow = range.Item1; rootRow < range.Item2; rootRow++)
            {
                EnumerateRoot(rootRow);
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
            while (emitted < cap && materializedQueue.TryDequeue(out var sol)) { onMaterialized(sol); emitted++; }
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
                int r = rows[i]; if (r < 0) return false;
                int reflected = N - 1 - r;
                if (r > reflected) return true;
                if (r < reflected) break;
            }
        }
        if (!minimalityEnabled) return false;
        for (int i = 0; i <= depth; i++)
        {
            int a = rows[i]; if (a < 0) return false;
            int b = rows[depth - i]; if (b < 0) return false;
            int transformed = N - 1 - b;
            if (a > transformed) return true;
            if (a < transformed) break;
        }
        return false;
    }
}
