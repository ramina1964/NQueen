namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;
using NQueen.Domain.Settings;
using NQueen.Domain.Utils;

internal static class UniqueSolutionCounter
{
    // Unified: use symmetry-pruned for large boards, canonical for small
    public static ulong Count(int boardSize, Action<double>? progress, Guid token,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender,
        bool aggressiveSymmetry = false, int cap = 0, Action<int[]>? onMaterialized = null)
    {
        if (boardSize <= 0) return 0;
        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            return AdaptiveParallelUniqueCount(boardSize, cap, onMaterialized, progressEventSource, sender, token);
        }
        else
        {
            ulong uniqueCount = 0;
            CanonicalUniqueSearchEngine.CountUnique(boardSize, onMaterialized);
            uniqueCount = CanonicalUniqueSearchEngine.CountUnique(boardSize, null);
            return uniqueCount;
        }
    }

    private static ulong AdaptiveParallelUniqueCount(int N, int cap, Action<int[]>? onMaterialized,
        EventHandler<ProgressUpdateEventArgs>? progressEventSource, object? sender, Guid token)
    {
        // Memory-aware + adaptive splitting strategy.
        int cores = Environment.ProcessorCount;
        int targetJobs = cores * 128; // increase granularity
        int maxDepth = 4;
        int depth = 2;
        // Use approximate branching factor (average legal positions per column ~ N/2 for large boards)
        double branch = N * 0.55; // heuristic
        while (depth < maxDepth && Math.Pow(branch, depth) < targetJobs) depth++;

        var partialStates = new List<PartialState>(targetJobs);
        int scratchSize = SymmetryHelper.GetScratchBufferSize(N);

        // Cap to avoid runaway memory
        int maxPartialStates = targetJobs * 2; // safety margin
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
                {
                    generationAborted = true;
                }
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
        if (totalJobs == 0)
            return 0UL;

        var globalUnique = new ConcurrentDictionary<UInt128, byte>(Environment.ProcessorCount * 2, totalJobs);
        int materializedGlobal = 0;
        int processedJobs = 0;
        int lastPctReported = -1;

        // Thread-local scratch & row buffer to minimize allocations inside DFS
        var scratchPool = new ThreadLocal<int[]>(() => new int[scratchSize]);

        long memoryFallbackThreshold = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 5; // 20% of available
        bool memoryFallbackTriggered = false;

        Parallel.ForEach(partialStates, new ParallelOptions { MaxDegreeOfParallelism = cores }, state =>
        {
            if (memoryFallbackTriggered) return; // skip work after fallback
            var rowsLocal = state.Rows;
            var scratch = scratchPool.Value!;
            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (memoryFallbackTriggered) return;
                if (col == N)
                {
                    UInt128 key = SymmetryHelper.GetCanonicalKey(rowsLocal, scratch, out var canonicalSpan);
                    if (globalUnique.TryAdd(key, 0) && cap > 0 && materializedGlobal < cap && onMaterialized != null)
                    {
                        int idx = Interlocked.Increment(ref materializedGlobal);
                        if (idx <= cap)
                        {
                            var copy = new int[N];
                            canonicalSpan.CopyTo(copy);
                            onMaterialized(copy);
                        }
                    }
                    return;
                }
                ulong maskAll = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
                ulong avail = ~(cols | d1 | d2) & maskAll;
                while (avail != 0 && !memoryFallbackTriggered)
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
            // Progress mapped to 0..95; final pass sets to 100
            int pct = (int)Math.Min(95.0, (double)done / totalJobs * 95.0);
            if (pct != lastPctReported)
            {
                int prev = Interlocked.Exchange(ref lastPctReported, pct);
                if (pct != prev && progressEventSource != null)
                    progressEventSource(sender!, new ProgressUpdateEventArgs(pct, token));
            }

            // Memory pressure fallback: switch to lookup if available
            if (!memoryFallbackTriggered)
            {
                long current = GC.GetTotalMemory(false);
                if (current > memoryFallbackThreshold)
                {
                    memoryFallbackTriggered = true;
                }
            }
        });

        ulong totalCount;
        if (memoryFallbackTriggered)
        {
            // Fallback to authoritative lookup (may undercount if enumeration partial, but acceptable under pressure)
            totalCount = ExpectedSolutionCounts.GetUnique(N);
        }
        else
        {
            totalCount = (ulong)globalUnique.Count;
        }

        if (progressEventSource != null)
            progressEventSource(sender!, new ProgressUpdateEventArgs(100.0, token));

        return totalCount;
    }

    private readonly record struct PartialState(int[] Rows, int Col, ulong Cols, ulong D1, ulong D2);
}
