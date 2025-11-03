namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public static void RunUnique(UniqueRequest request)
    {
        int N = request.BoardSize; request.ReportProgress(0.0);
        ulong fundamentalCount = 0;
        var globalUnique = new ConcurrentDictionary<UInt128, byte>();
        var tasks = new List<Task>();
        int materializedCount = 0;
        int cap = request.ShouldMaterialize() ? SimulationSettings.MaxDisplayedCount : 0;
        int rootsCompleted = 0;

        // Enumerate all roots (legacy half-root logic removed for correctness & simplicity)
        for (int fr = 0; fr < N; fr++)
        {
            int root = fr;
            tasks.Add(Task.Run(() => EnumerateRoot(root)));
        }
        Task.WaitAll(tasks.ToArray());
        fundamentalCount = (ulong)globalUnique.Count;

        request.ReportProgress(100.0);
        request.OnCompletedUniqueCount(fundamentalCount);

        void EnumerateRoot(int fr)
        {
            var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
            var rowsArr = new int[N];
            Array.Fill(rowsArr, -1);
            rowsArr[0] = fr;
            ulong bitFirst = 1UL << fr;
            ulong cols = bitFirst;
            ulong d1 = bitFirst << 1;
            ulong d2 = bitFirst >> 1;
            ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
            ulong[] stackCols = new ulong[N];
            ulong[] stackD1 = new ulong[N];
            ulong[] stackD2 = new ulong[N];
            ulong[] stackRemaining = new ulong[N];
            int col = 1;
            ulong remaining = ComputeAvail(col);
            while (true)
            {
                if (col == N)
                {
                    // Always canonicalize (rootIsCanonical shortcut removed)
                    UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out var canonicalSpan);
                    if (globalUnique.TryAdd(key, 0) && materializedCount < cap)
                    {
                        int newVal = Interlocked.Increment(ref materializedCount);
                        if (newVal <= cap)
                        {
                            int[] canonicalRows = new int[N];
                            canonicalSpan.CopyTo(canonicalRows);
                            request.OnUniqueSolution(canonicalRows);
                        }
                    }
                    col--; if (col <= 0) break; Restore(col, out remaining); continue;
                }
                if (remaining == 0)
                {
                    col--; if (col <= 0) break; Restore(col, out remaining); continue;
                }
                ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit;
                int row = BitOperations.TrailingZeroCount(bit);
                rowsArr[col] = row;
                stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1;
                col++;
                if (col == N) continue;
                remaining = ComputeAvail(col);
            }
            if (request.EnableEvents)
            {
                int done = Interlocked.Increment(ref rootsCompleted);
                double pctBase = (double)done / N * 100.0;
                request.ReportProgress(Math.Min(100.0, pctBase));
            }
            ulong ComputeAvail(int c)
            {
                ulong avail = ~(cols | d1 | d2) & mask;
                // Removed small-board first-column pruning to avoid missing canonical representatives
                return avail;
            }
            void Restore(int c, out ulong rem)
            {
                rem = stackRemaining[c];
                cols = stackCols[c]; d1 = stackD1[c]; d2 = stackD2[c];
            }
        }
    }

    // Unified unique solution search: materialize up to cap, then count only, with global early termination
    public static void RunUniqueUnified(
        int boardSize,
        bool enableEvents,
        int cap,
        Action<int[]> onUniqueSolution,
        Action<ulong> onCompletedUniqueCount,
        Action<double> reportProgress,
        Func<bool> capReached)
    {
        int N = boardSize;
        reportProgress(0.0);
        var globalUnique = new ConcurrentDictionary<UInt128, byte>();
        var tasks = new List<Task>();
        int materializedCount = 0;
        int rootsCompleted = 0;

        // Enumerate all roots (legacy half-root logic removed)
        for (int fr = 0; fr < N; fr++)
        {
            int root = fr;
            tasks.Add(Task.Run(() => EnumerateRoot(root)));
        }
        Task.WaitAll(tasks.ToArray());

        reportProgress(100.0);
        onCompletedUniqueCount((ulong)globalUnique.Count);

        void EnumerateRoot(int fr)
        {
            var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
            var rowsArr = new int[N];
            Array.Fill(rowsArr, -1);
            rowsArr[0] = fr;
            ulong bitFirst = 1UL << fr;
            ulong cols = bitFirst;
            ulong d1 = bitFirst << 1;
            ulong d2 = bitFirst >> 1;
            ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
            ulong[] stackCols = new ulong[N];
            ulong[] stackD1 = new ulong[N];
            ulong[] stackD2 = new ulong[N];
            ulong[] stackRemaining = new ulong[N];
            int col = 1;
            ulong remaining = ComputeAvail(col);
            while (true)
            {
                if (col == N)
                {
                    UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratchBuf, out var canonicalSpan);
                    if (globalUnique.TryAdd(key, 0))
                    {
                        int mat = Interlocked.Increment(ref materializedCount);
                        if (mat <= cap)
                        {
                            int[] canonicalRows = new int[N];
                            canonicalSpan.CopyTo(canonicalRows);
                            onUniqueSolution(canonicalRows);
                        }
                        if (capReached()) { /* early termination hook retained */ }
                    }
                    col--; if (col <= 0) break; Restore(col, out remaining); continue;
                }
                if (remaining == 0)
                {
                    col--; if (col <= 0) break; Restore(col, out remaining); continue;
                }
                ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit;
                int row = BitOperations.TrailingZeroCount(bit);
                rowsArr[col] = row;
                stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1;
                col++;
                if (col == N) continue;
                remaining = ComputeAvail(col);
            }
            if (enableEvents)
            {
                int done = Interlocked.Increment(ref rootsCompleted);
                double pctBase = (double)done / N * 100.0;
                reportProgress(Math.Min(100.0, pctBase));
            }
            ulong ComputeAvail(int c)
            {
                ulong avail = ~(cols | d1 | d2) & mask;
                return avail; // pruning removed
            }
            void Restore(int c, out ulong rem)
            {
                rem = stackRemaining[c];
                cols = stackCols[c]; d1 = stackD1[c]; d2 = stackD2[c];
            }
        }
    }
}
