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

        Parallel.For(0, N, rootRow =>
        {
            // Per-task state
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            rows[0] = rootRow;
            int[] scratch = new int[N * 8];
            int localMaterialized = 0;
            ulong localCount = 0UL;

            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    // Canonical minimality test
                    var canon = SymmetryHelper.GetCanonicalForm(rows, scratch, null);
                    bool minimal = true;
                    for (int i = 0; i < N; i++)
                    {
                        if (rows[i] != canon[i]) { minimal = false; break; }
                    }
                    if (minimal)
                    {
                        localCount++;
                        if (materializedQueue != null && localMaterialized < cap && onMaterialized != null)
                        {
                            var copy = new int[N];
                            Array.Copy(rows, copy, N);
                            materializedQueue.Enqueue(copy);
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
                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    rows[col] = -1;
                }
            }

            ulong bit0 = 1UL << rootRow;
            DFS(1, bit0, bit0 << 1, bit0 >> 1);
            System.Threading.Interlocked.Add(ref totalCount, localCount);
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
}
