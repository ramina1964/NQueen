namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;
using NQueen.Domain.Settings;
using NQueen.Domain.Utils;
using System.Collections.Concurrent;

internal static class UniqueSolutionCounter
{
    // Unified entry point
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false, int cap = 0, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0;
        bool countOnly = cap <= 0 || onMaterialized == null;

        // Large boards path
        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            // Use symmetry-pruned counter for consistent counts between modes
            return SymmetryPrunedUniqueCounter.Count(boardSize, cap, onMaterialized);
        }

        // Small boards path
        if (countOnly)
        {
            // Canonical minimality check enumeration without storing keys
            return CanonicalCountMinimalOnly(boardSize);
        }
        else
        {
            // Materialize via canonical enumerator until cap reached
            int emitted = 0;
            ulong total = CanonicalMinimalMaterialize(boardSize, cap, onMaterialized!, ref emitted);
            return total;
        }
    }

    // Small-board minimality counting (no HashSet) – brute force
    private static ulong CanonicalCountMinimalOnly(int N)
    {
        if (N <= 0) return 0;
        ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int[] rows = new int[N];
        int[] scratch = new int[N * 8];
        Array.Fill(rows, -1);
        ulong count = 0;
        void DFS(int col, ulong cols, ulong d1, ulong d2)
        {
            if (col == N)
            {
                var canon = SymmetryHelper.GetCanonicalForm(rows, scratch, null);
                bool minimal = true;
                for (int i = 0; i < N; i++)
                {
                    if (rows[i] != canon[i]) { minimal = false; break; }
                }
                if (minimal) count++;
                return;
            }
            ulong avail = ~(cols | d1 | d2) & fullMask;
            while (avail != 0)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int r = BitOperations.TrailingZeroCount(bit);
                rows[col] = r;
                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                rows[col] = -1;
            }
        }
        DFS(0, 0, 0, 0);
        return count;
    }

    private static ulong CanonicalMinimalMaterialize(int N, int cap, Action<int[]> onMaterialized, ref int emitted)
    {
        if (N <= 0) return 0;
        ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int[] rows = new int[N];
        int[] scratch = new int[N * 8];
        Array.Fill(rows, -1);
        ulong count = 0;
        int localEmitted = emitted; // copy for local function capture
        void DFS(int col, ulong cols, ulong d1, ulong d2)
        {
            if (col == N)
            {
                var canon = SymmetryHelper.GetCanonicalForm(rows, scratch, null);
                bool minimal = true;
                for (int i = 0; i < N; i++)
                {
                    if (rows[i] != canon[i]) { minimal = false; break; }
                }
                if (minimal)
                {
                    count++;
                    if (localEmitted < cap)
                    {
                        var copy = new int[N];
                        Array.Copy(rows, copy, N);
                        onMaterialized(copy);
                        localEmitted++;
                    }
                }
                return;
            }
            ulong avail = ~(cols | d1 | d2) & fullMask;
            while (avail != 0 && localEmitted < cap)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int r = BitOperations.TrailingZeroCount(bit);
                rows[col] = r;
                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                rows[col] = -1;
            }
            // If cap reached, we still need to finish enumeration (without materializing) to get accurate count
            while (avail != 0 && localEmitted >= cap)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int r = BitOperations.TrailingZeroCount(bit);
                rows[col] = r;
                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                rows[col] = -1;
            }
        }
        DFS(0, 0, 0, 0);
        emitted = localEmitted; // write back
        return count;
    }

    private readonly record struct PartialState(int[] PrefixRows, int Col, ulong Cols, ulong D1, ulong D2);
}
