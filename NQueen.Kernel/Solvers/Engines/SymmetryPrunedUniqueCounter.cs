namespace NQueen.Kernel.Solvers.Engines;

/// <summary>
/// Memory-efficient symmetry-pruned unique solution counter for large boards.
/// Materializes up to a cap, then continues counting without storing further solutions.
/// </summary>
public static class SymmetryPrunedUniqueCounter
{
    public static ulong Count(int boardSize, int cap, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0;
        int N = boardSize;
        ulong count = 0;
        int materialized = 0;
        int[] rows = new int[N];
        Array.Fill(rows, -1);

        // Helper: DFS with symmetry reduction (first row only up to half for unique reps)
        void DFS(int col, ulong cols, ulong d1, ulong d2)
        {
            if (col == N)
            {
                count++;
                if (materialized < cap && onMaterialized != null)
                {
                    var copy = new int[N];
                    Array.Copy(rows, copy, N);
                    onMaterialized(copy);
                    materialized++;
                }
                return;
            }
            ulong avail = ~(cols | d1 | d2) & ((1UL << N) - 1UL);
            if (col == 0)
            {
                // Only place first queen in first half (symmetry reduction)
                int maxRow = (N + 1) / 2;
                avail &= (1UL << maxRow) - 1UL;
            }
            for (int row = 0; row < N; row++)
            {
                ulong bit = 1UL << row;
                if ((avail & bit) == 0) continue;
                rows[col] = row;
                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                rows[col] = -1;
            }
        }
        DFS(0, 0, 0, 0);
        // Multiply by symmetry factor
        if ((N & 1) == 0)
        {
            count *= 2;
        }
        else
        {
            // For odd N, double and add center solutions
            int center = N / 2;
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
                        onMaterialized(copy);
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
        }
        return count;
    }
}
