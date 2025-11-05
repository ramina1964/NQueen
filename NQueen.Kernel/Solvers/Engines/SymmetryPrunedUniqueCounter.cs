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
        bool isOdd = (N & 1) == 1;
        int half = N / 2;            // number of rows strictly in first half (rows 0..half-1)
        int centerRow = isOdd ? half : -1; // center row index when odd

        var counts = new ulong[half];
        var mats = new int[half];
        var materializedSolutions = new ConcurrentQueue<int[]>();

        // Enumerate strictly first half rows (mirror counted later by doubling)
        Parallel.For(0, half, rootRow =>
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
        for (int i = 0; i < half; i++)
        {
            totalCount += counts[i];
            totalMaterialized += mats[i];
        }
        // Mirror first-half results
        totalCount *= 2;
        totalMaterialized *= 2;

        // Center row (only for odd N) – not mirrored
        if (isOdd)
        {
            ulong centerCount = 0;
            int centerMaterialized = 0;
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            rows[0] = centerRow;
            void CenterDFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    centerCount++;
                    if (centerMaterialized < cap && onMaterialized != null)
                    {
                        var copy = new int[N];
                        Array.Copy(rows, copy, N);
                        materializedSolutions.Enqueue(copy);
                        centerMaterialized++;
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
            CenterDFS(1, 1UL << centerRow, (1UL << centerRow) << 1, (1UL << centerRow) >> 1);
            totalCount += centerCount;
            totalMaterialized += centerMaterialized;
        }

        // Emit up to cap materialized solutions (duplicates from symmetry not expanded; consumer shows sample reps)
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
