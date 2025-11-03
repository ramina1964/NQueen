namespace NQueen.Kernel.Solvers.Engines;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

/// <summary>
/// Memory-efficient symmetry-pruned unique solution counter for large boards.
/// Materializes up to a cap, then continues counting without storing further solutions.
/// Parallelized at the first queen's position (first row).
/// </summary>
public static class SymmetryPrunedUniqueCounter
{
    public static ulong Count(int boardSize, int cap, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0;
        int N = boardSize;
        int maxRow = (N + 1) / 2;
        var counts = new ulong[maxRow];
        var mats = new int[maxRow];
        var materializedSolutions = new ConcurrentQueue<int[]>();

        Parallel.For(0, maxRow, rootRow =>
        {
            ulong count = 0;
            int materialized = 0;
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            rows[0] = rootRow;
            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    count++;
                    if (materialized < cap && onMaterialized != null)
                    {
                        var copy = new int[N];
                        Array.Copy(rows, copy, N);
                        materializedSolutions.Enqueue(copy);
                        materialized++;
                    }
                    return;
                }
                ulong avail = ~(cols | d1 | d2) & ((1UL << N) - 1UL);
                for (int row = 0; row < N; row++)
                {
                    ulong bit = 1UL << row;
                    if ((avail & bit) == 0) continue;
                    rows[col] = row;
                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    rows[col] = -1;
                }
            }
            DFS(1, 1UL << rootRow, (1UL << rootRow) << 1, (1UL << rootRow) >> 1);
            counts[rootRow] = count;
            mats[rootRow] = materialized;
        });

        ulong totalCount = 0;
        int totalMaterialized = 0;
        for (int i = 0; i < maxRow; i++)
        {
            totalCount += counts[i];
            totalMaterialized += mats[i];
        }
        totalCount *= 2;
        totalMaterialized *= 2;

        // For odd N, handle center row separately
        if ((N & 1) == 1)
        {
            int center = N / 2;
            ulong count = 0;
            int materialized = 0;
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            rows[0] = center;
            void CenterDFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    count++;
                    if (materialized < cap && onMaterialized != null)
                    {
                        var copy = new int[N];
                        Array.Copy(rows, copy, N);
                        materializedSolutions.Enqueue(copy);
                        materialized++;
                    }
                    return;
                }
                ulong avail = ~(cols | d1 | d2) & ((1UL << N) - 1UL);
                for (int row = 0; row < N; row++)
                {
                    ulong bit = 1UL << row;
                    if ((avail & bit) == 0) continue;
                    rows[col] = row;
                    CenterDFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    rows[col] = -1;
                }
            }
            CenterDFS(1, 1UL << center, (1UL << center) << 1, (1UL << center) >> 1);
            totalCount += count;
            totalMaterialized += materialized;
        }

        // Emit up to cap materialized solutions
        if (onMaterialized != null && cap > 0)
        {
            int emitted = 0;
            while (emitted < cap && materializedSolutions.TryDequeue(out var sol))
            {
                onMaterialized(sol);
                emitted++;
            }
        }
        return totalCount;
    }
}
