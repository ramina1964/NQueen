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
        int targetJobs = cores * 256;
        int maxDepth = 5;
        int depth = 2;
        double branch = N * 0.55;
        while (depth < maxDepth && Math.Pow(branch, depth) < targetJobs) depth++;

        // Optimization 2: store only prefix rows (compressed) instead of full N snapshot
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
                // copy only prefix of length col
                var prefix = new int[col];
                if (col > 0) Array.Copy(rows, prefix, col);
                partialStates.Add(new PartialState(prefix, col, cols, d1, d2));
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
        // Optimization 4: dictionary only needed while we are still materializing (before cap reached)
        var globalUnique = materialize ? new ConcurrentDictionary<UInt128, byte>(cores * 2, totalJobs) : null;
        int materializedGlobal = 0;
        int processedJobs = 0;
        int lastPctReported = -1;
        var scratchPool = new ThreadLocal<int[]>(() => new int[scratchSize]);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        long lastEmitMs = 0;
        ulong estimatedTotal = ExpectedSolutionCounts.GetUnique(N);
        const double leafWeight = 0.35;

        void TryEmitProgress()
        {
            if (progressEventSource == null) return;
            double jobBasePct = totalJobs == 0 ? 0 : (double)processedJobs / totalJobs;
            double leafPct = 0;
            if (estimatedTotal > 0)
                leafPct = Math.Min(1.0, (double)minimalCount / estimatedTotal);
            double blended = Math.Min(0.98, jobBasePct * (1.0 - leafWeight) + leafPct * leafWeight);
            int pct = (int)Math.Max(lastPctReported, blended * 98.0);
            if (pct > 98) pct = 98;
            if (pct != lastPctReported)
            {
                lastPctReported = pct;
                progressEventSource(sender!, new ProgressUpdateEventArgs(pct, token));
                lastEmitMs = sw.ElapsedMilliseconds;
            }
            else if (sw.ElapsedMilliseconds - lastEmitMs >= SimulationSettings.ProgressIntervalInMilliSec && lastPctReported < 98)
            {
                lastPctReported++;
                progressEventSource(sender!, new ProgressUpdateEventArgs(lastPctReported, token));
                lastEmitMs = sw.ElapsedMilliseconds;
            }
        }

        Parallel.ForEach(partialStates, new ParallelOptions { MaxDegreeOfParallelism = cores }, state =>
        {
            // reconstruct full rows array from prefix
            var rowsLocal = new int[N];
            Array.Fill(rowsLocal, -1);
            if (state.PrefixRows.Length > 0)
                Array.Copy(state.PrefixRows, rowsLocal, state.PrefixRows.Length);
            var scratch = scratchPool.Value!;
            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    // If cap reached (materialization done), avoid key packing & dictionary insert
                    if (materialize && materializedGlobal < cap)
                    {
                        UInt128 key = SymmetryHelper.GetCanonicalKey(rowsLocal, scratch, out var canonicalSpan);
                        bool isMinimal = true;
                        for (int i = 0; i < N; i++)
                        {
                            if (rowsLocal[i] != canonicalSpan[i]) { isMinimal = false; break; }
                        }
                        if (!isMinimal) return;
                        if (globalUnique!.TryAdd(key, 0))
                        {
                            if (materializedGlobal < cap && onMaterialized != null)
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
                    }
                    else
                    {
                        // Minimality test without dictionary & packing
                        var canon = SymmetryHelper.GetCanonicalForm(rowsLocal, scratch, null);
                        bool minimal = true;
                        for (int i = 0; i < N; i++)
                        {
                            if (rowsLocal[i] != canon[i]) { minimal = false; break; }
                        }
                        if (!minimal) return;
                    }
                    Interlocked.Increment(ref minimalCount);
                    if ((minimalCount & 0xFFF) == 0) TryEmitProgress();
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
            Interlocked.Increment(ref processedJobs);
            if ((processedJobs & 0x3F) == 0) TryEmitProgress();
        });

        // Final emit 100%
        progressEventSource?.Invoke(sender!, new ProgressUpdateEventArgs(100.0, token));
        return minimalCount;
    }

    private readonly record struct PartialState(int[] PrefixRows, int Col, ulong Cols, ulong D1, ulong D2);
}
