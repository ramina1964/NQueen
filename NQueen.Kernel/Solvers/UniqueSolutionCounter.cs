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
            if (countOnly)
            {
                // Use same adaptive canonical minimal enumeration to keep counts consistent with materialize path
                return AdaptiveParallelUniqueCount(boardSize, 0, null, progressEventSource, sender, token);
            }
            return AdaptiveParallelUniqueCount(boardSize, cap, onMaterialized, progressEventSource, sender, token);
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

    private static ulong AdaptiveParallelUniqueCount(int N, int cap, Action<int[]>? onMaterialized,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender, Guid token)
    {
        bool materialize = cap > 0 && onMaterialized != null;
        int cores = Environment.ProcessorCount;
        int targetJobs = cores * 256; // increased granularity (was cores*128)
        int maxDepth = 5; // allow one more level to create more partial states
        int depth = 2;
        double branch = N * 0.55;
        while (depth < maxDepth && Math.Pow(branch, depth) < targetJobs) depth++;

        var partialStates = new List<PartialState>(targetJobs);
        int scratchSize = SymmetryHelper.GetScratchBufferSize(N);
        int maxPartialStates = targetJobs * 2;
        bool generationAborted = false;

        for (int rootRow = 0; rootRow < N && !generationAborted; rootRow++)
        {
            int[] rows = new int[N];
            Array.Fill(rows, -1);
            rows[0] = rootRow;
            ulong bit = 1UL << rootRow;
            DFSGenerate(1, bit, bit << 1, bit >> 1, rows, depth);
        }

        void DFSGenerate(int col, ulong cols, ulong d1, ulong d2, int[] rows, int maxDepthLocal)
        {
            if (generationAborted) return;
            if (col == N || col == maxDepthLocal)
            {
                var snap = new int[N];
                Array.Copy(rows, snap, N);
                partialStates.Add(new PartialState(snap, col, cols, d1, d2));
                if (partialStates.Count >= maxPartialStates)
                    generationAborted = true;
                return;
            }
            ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
            ulong avail = ~(cols | d1 | d2) & mask;
            while (avail != 0 && !generationAborted)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                int row = BitOperations.TrailingZeroCount(bit);
                rows[col] = row;
                DFSGenerate(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1, rows, maxDepthLocal);
                rows[col] = -1;
            }
        }

        int totalJobs = partialStates.Count;
        if (totalJobs == 0) return 0UL;

        ulong minimalCount = 0;
        var globalUnique = materialize ? new ConcurrentDictionary<UInt128, byte>(cores * 2, totalJobs) : null;
        int materializedGlobal = 0;
        int processedJobs = 0;
        int lastPctReported = -1;
        var scratchPool = new ThreadLocal<int[]>(() => new int[scratchSize]);

        Parallel.ForEach(partialStates, new ParallelOptions { MaxDegreeOfParallelism = cores }, state =>
        {
            var rowsLocal = state.Rows;
            var scratch = scratchPool.Value!;
            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    UInt128 key = SymmetryHelper.GetCanonicalKey(rowsLocal, scratch, out var canonicalSpan);
                    bool isMinimal = true;
                    for (int i = 0; i < N; i++)
                    {
                        if (rowsLocal[i] != canonicalSpan[i]) { isMinimal = false; break; }
                    }
                    if (!isMinimal) return;

                    if (materialize)
                    {
                        if (globalUnique!.TryAdd(key, 0) && materializedGlobal < cap && onMaterialized != null)
                        {
                            int idx = Interlocked.Increment(ref materializedGlobal);
                            if (idx <= cap)
                            {
                                var copy = new int[N];
                                canonicalSpan.CopyTo(copy);
                                onMaterialized(copy);
                            }
                        }
                    }
                    Interlocked.Increment(ref minimalCount);
                    return;
                }
                ulong maskAll = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
                ulong avail = ~(cols | d1 | d2) & maskAll;
                while (avail != 0)
                {
                    ulong bit = avail & (ulong)-(long)avail;
                    avail ^= bit;
                    int row = BitOperations.TrailingZeroCount(bit);
                    rowsLocal[col] = row;
                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    rowsLocal[col] = -1;
                }
            }
            DFS(state.Col, state.Cols, state.D1, state.D2);

            int done = Interlocked.Increment(ref processedJobs);
            int pct = (int)Math.Min(98.0, (double)done / totalJobs * 98.0); // use 98% span, reserve 2% for final aggregation
            if (pct != lastPctReported)
            {
                int prev = Interlocked.Exchange(ref lastPctReported, pct);
                if (pct != prev && progressEventSource != null)
                    progressEventSource(sender!, new ProgressUpdateEventArgs(pct, token));
            }
        });

        progressEventSource?.Invoke(sender!, new ProgressUpdateEventArgs(100.0, token));
        return minimalCount;
    }

    private readonly record struct PartialState(int[] Rows, int Col, ulong Cols, ulong D1, ulong D2);
}
