using System.Buffers;

namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    internal static class UniqueInstrumentation
    {
        // Set Enabled to true if you want instrumentation code to run.
        public const bool Enabled = true;

        private static long _nodeVisits;

        private static long _leaves;

        private static long _prefixPruned;

        public static void Reset()
        {
            _nodeVisits = 0;
            _leaves = 0;
            _prefixPruned = 0;
        }

        public static void VisitNode()
        {
            if (Enabled)
                Interlocked.Increment(ref _nodeVisits);
        }

        public static void VisitLeaf()
        {
            if (Enabled)
                Interlocked.Increment(ref _leaves);
        }

        public static void PrefixPrune()
        {
            if (Enabled)
                Interlocked.Increment(ref _prefixPruned);
        }

        public static (long nodes, long leaves, long pruned) Snapshot() =>
            (
                Interlocked.Read(ref _nodeVisits),
                Interlocked.Read(ref _leaves),
                Interlocked.Read(ref _prefixPruned)
            );
    }

    private const int DepthSplitThresholdN = 16;
    private const int DepthSplitLevel = 2;
    private const int PrefixPruneStartDepth = 4;
    private const int PrefixPruneThresholdN = 18;
    private const bool EnableSecondColumnPrune = true;
    private const bool EnablePrefixPrune = true;
    private const bool ThrottleProgressLargeBoards = true;

    public static void RunUnique(UniqueRequest request)
    {
        System.Threading.ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);

        int N = request.BoardSize; request.ReportProgress(0.0);
        if (N <= 0) { request.OnCompletedUniqueCount(0); return; }
        var globalUnique = new ConcurrentDictionary<UInt128, byte>();
        int materializedCount = 0;
        int cap = request.ShouldMaterialize() ? SimulationSettings.MaxDisplayedCount : 0;
        ulong expectedTotal = N <= 29 ? ExpectedSolutionCounts.GetUniqueFast(N) : 0UL;

        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        ulong[] rowBits = new ulong[N];
        for (int r = 0; r < N; r++) rowBits[r] = 1UL << r;
        // Precompute lower masks for second-column pruning
        // REVERT: remove cached masks and compute inline to match prior behavior
        // ulong[] lowerMasks = new ulong[N];
        // for (int r = 0; r < N; r++)
        // {
        //     int minRow = r + 1;
        //     lowerMasks[r] = minRow < N ? ((1UL << minRow) - 1UL) : ((1UL << N) - 1UL);
        // }

        var partialStates = new List<PartialState>();

        // Use the new split depth calculation
        int splitDepth = (N >= SimulationSettings.DynamicRootSplitLimitN)
            ? SimulationSettings.CalculateSplitDepth(N)
            : ((N >= DepthSplitThresholdN && DepthSplitLevel > 0)
                ? DepthSplitLevel : 1);

        GeneratePartialStates(N, splitDepth, mask, partialStates);

        int totalTasks = partialStates.Count;
        int progressCounter = 0;
        int progressBucketReported = -1;
        // Use 1% bucket for smoother progress on large unique runs
        int progressBucketSize = 1;
        var lastHeartbeat = System.Diagnostics.Stopwatch.StartNew();
        const int heartbeatMs = 1500;

        if (N >= 19)
        {
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(partialStates, parallelOptions, ps =>
            {
                EnumerateFromPartial(ps);
                int done = Interlocked.Increment(ref progressCounter);
                if (request.EnableEvents && expectedTotal == 0)
                {
                    double pct = (double)done / totalTasks * 100.0;
                    int bucket = (int)pct / progressBucketSize * progressBucketSize;
                    int observed;
                    while (bucket > (observed = Volatile.Read(ref progressBucketReported)))
                    {
                        if (Interlocked.CompareExchange(ref progressBucketReported, bucket, observed) == observed)
                        {
                            request.ReportProgress(bucket);
                            break;
                        }
                    }
                    if (lastHeartbeat.ElapsedMilliseconds >= heartbeatMs)
                    {
                        request.ReportProgress(Math.Min(99.0, pct));
                        lastHeartbeat.Restart();
                    }
                }
            });
        }
        else
        {
            // Bounded work-stealing queue to reduce Partitioner overhead for mid-range N
            var queue = new ConcurrentQueue<PartialState>();
            foreach (var ps in partialStates) queue.Enqueue(ps);
            int cores = Environment.ProcessorCount;
            int batchSize = Math.Max(8, Math.Max(1, queue.Count) / (cores * 8));
            var workers = new List<Task>(cores);

            for (int w = 0; w < cores; w++)
            {
                workers.Add(Task.Run(() =>
                {
                    var localBatch = new List<PartialState>(batchSize);
                    while (!queue.IsEmpty)
                    {
                        localBatch.Clear();
                        for (int i = 0; i < batchSize && queue.TryDequeue(out var item); i++)
                            localBatch.Add(item);
                        if (localBatch.Count == 0) break;

                        foreach (var ps in localBatch)
                        {
                            EnumerateFromPartial(ps);
                            int done = Interlocked.Increment(ref progressCounter);
                            if (request.EnableEvents && expectedTotal == 0)
                            {
                                double pct = (double)done / totalTasks * 100.0;
                                int bucket = (int)pct / progressBucketSize * progressBucketSize;
                                int observed;
                                while (bucket > (observed = Volatile.Read(ref progressBucketReported)))
                                {
                                    if (Interlocked.CompareExchange(ref progressBucketReported, bucket, observed) == observed)
                                    {
                                        request.ReportProgress(bucket);
                                        break;
                                    }
                                }
                                if (lastHeartbeat.ElapsedMilliseconds >= heartbeatMs)
                                {
                                    request.ReportProgress(Math.Min(99.0, pct));
                                    lastHeartbeat.Restart();
                                }
                            }
                        }
                    }
                }));
            }
            Task.WaitAll(workers.ToArray());
        }

        request.OnCompletedUniqueCount((ulong)globalUnique.Count);
        request.ReportProgress(100.0);

        void EnumerateFromPartial(PartialState ps)
        {
            // Expand prefix rows (length may be < N) into working N-sized array
            int[] rowsArr = new int[N];
            Array.Fill(rowsArr, -1);
            if (ps.Rows.Length > 0)
                Array.Copy(ps.Rows, 0, rowsArr, 0, ps.Rows.Length);
            int col = ps.Depth;
            ulong cols = ps.Cols;
            ulong d1 = ps.D1;
            ulong d2 = ps.D2;
            ulong remaining = ~(cols | d1 | d2) & mask;
            if (EnableSecondColumnPrune && col == 1)
            {
                int firstRow = rowsArr[0];
                if (!((N & 1) == 1 && firstRow == N / 2))
                {
                    ulong lowerMask = (1UL << (firstRow + 1)) - 1UL;
                    remaining &= ~lowerMask;
                }
            }

            ulong[] stackCols = new ulong[N];
            ulong[] stackD1 = new ulong[N];
            ulong[] stackD2 = new ulong[N];
            ulong[] stackRemaining = new ulong[N];
            int[] scratch = ArrayPool<int>.Shared.Rent(N * 8);
            try
            {
                while (true)
                {
                    UniqueInstrumentation.VisitNode();
                    if (col == N)
                    {
                        UniqueInstrumentation.VisitLeaf();
                        UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratch, out var canonicalSpan);
                        if (globalUnique.TryAdd(key, 0))
                        {
                            if (materializedCount < cap)
                            {
                                int newVal = Interlocked.Increment(ref materializedCount);
                                if (newVal <= cap)
                                {
                                    int[] canonicalRows = new int[N];
                                    canonicalSpan.CopyTo(canonicalRows);
                                    request.OnUniqueSolution(canonicalRows);
                                }
                            }
                            if (request.EnableEvents && expectedTotal > 0)
                            {
                                ulong uniqueSoFar = (ulong)globalUnique.Count;
                                double pctSol = (double)uniqueSoFar / expectedTotal * 100.0;
                                if (pctSol > 99.0) pctSol = 99.0;
                                if (pctSol >= progressBucketReported + progressBucketSize || lastHeartbeat.ElapsedMilliseconds >= heartbeatMs)
                                {
                                    int bucket = (int)pctSol;
                                    int observed = Volatile.Read(ref progressBucketReported);
                                    if (bucket > observed && Interlocked.CompareExchange(ref progressBucketReported, bucket, observed) == observed)
                                    {
                                        request.ReportProgress(bucket);
                                        lastHeartbeat.Restart();
                                    }
                                    else if (lastHeartbeat.ElapsedMilliseconds >= heartbeatMs)
                                    {
                                        request.ReportProgress(Math.Min(99.0, pctSol));
                                        lastHeartbeat.Restart();
                                    }
                                }
                            }
                        }
                        col--;
                        if (col < ps.Depth)
                            break;

                        Restore(col, out remaining);
                        continue;
                    }
                    if (remaining == 0)
                    {
                        col--;
                        if (col < ps.Depth) break;
                        Restore(col, out remaining);
                        continue;
                    }

                    ulong bit = remaining & (ulong)-(long)remaining;
                    remaining ^= bit;
                    int row = BitOperations.TrailingZeroCount(bit);
                    rowsArr[col] = row;
                    stackCols[col] = cols; stackD1[col] = d1;
                    stackD2[col] = d2; stackRemaining[col] = remaining;
                    cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1;
                    col++;

                    if (col == N) continue;
                    remaining = ~(cols | d1 | d2) & mask;
                    if (EnableSecondColumnPrune && col == 1)
                    {
                        int firstRow = rowsArr[0];
                        if (((N & 1) == 1 && firstRow == N / 2) == false)
                        {
                            ulong lowerMask = (1UL << (firstRow + 1)) - 1UL;
                            remaining &= ~lowerMask;
                        }
                    }
                    if (EnablePrefixPrune && N >= PrefixPruneThresholdN && col >= PrefixPruneStartDepth)
                    {
                        if (IdentityPrefixMinimal(rowsArr, col, scratch, N) == false)
                        {
                            UniqueInstrumentation.PrefixPrune();
                            col--;
                            Restore(col, out remaining);
                            continue;
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(scratch, clearArray: UniqueInstrumentation.Enabled);
            }
            void Restore(int c, out ulong rem)
            {
                rem = stackRemaining[c]; cols = stackCols[c]; d1 = stackD1[c]; d2 = stackD2[c];
            }
        }
    }
    public static void RunUniqueUnified(
        int boardSize, bool enableEvents, int cap,
        Action<int[]> onUniqueSolution, Action<ulong> onCompletedUniqueCount,
        Action<double> reportProgress, Func<bool> capReached)
    {
        var req = new UniqueRequest(boardSize, enableEvents, () =>
            cap > 0, onUniqueSolution, onCompletedUniqueCount, reportProgress);

        RunUnique(req);
    }

    private static void GeneratePartialStates(int N,
        int splitDepth, ulong mask, List<PartialState> dest)
    {
        int[] rows = new int[N]; System.Array.Fill(rows, -1);
        ulong cols = 0UL; ulong d1 = 0UL;
        ulong d2 = 0UL; int depth = 0;
        ulong[] stackCols = new ulong[N];
        ulong[] stackD1 = new ulong[N];
        ulong[] stackD2 = new ulong[N];
        ulong[] stackAvail = new ulong[N];
        ulong avail = mask; // all rows available initially
        while (true)
        {
            if (depth == splitDepth)
            {
                // Store only the active prefix to reduce copy and memory footprint
                var prefix = new int[depth];
                if (depth > 0) Array.Copy(rows, 0, prefix, 0, depth);
                dest.Add(new PartialState(prefix, depth, cols, d1, d2));
                depth--;
                if (depth < 0)
                    break;

                Restore(depth);
                continue;
            }
            if (avail == 0UL)
            {
                depth--;
                if (depth < 0)
                    break;

                Restore(depth); continue;
            }
            ulong bit = avail & (ulong)-(long)avail;
            avail ^= bit;
            int row = BitOperations.TrailingZeroCount(bit);
            rows[depth] = row;
            stackCols[depth] = cols;
            stackD1[depth] = d1;
            stackD2[depth] = d2;
            stackAvail[depth] = avail;
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1; depth++;

            if (depth == splitDepth)
            {
                avail = 0UL; continue;
            }

            avail = ~(cols | d1 | d2) & mask;
            if (EnableSecondColumnPrune && depth == 1)
            {
                int firstRow = rows[0];
                if (((N & 1) == 1 && firstRow == N / 2) == false)
                {
                    ulong lowerMask = (1UL << (firstRow + 1)) - 1UL;
                    avail &= ~lowerMask;
                }
            }
        }
        void Restore(int c)
        {
            avail = stackAvail[c];
            cols = stackCols[c];
            d1 = stackD1[c];
            d2 = stackD2[c]; rows[c] = -1;
        }
    }

    private static bool IdentityPrefixMinimal(int[] rows, int depth, int[] scratch, int N)
    {
        for (int t = 0; t < 8; t++)
        {
            int baseOffset = t * N;
            for (int i = 0; i < depth; i++)
                scratch[baseOffset + i] = int.MaxValue;
        }

        for (int c = 0; c < depth; c++)
        {
            int r = rows[c];
            if (r < 0)
                continue;

            scratch[0 * N + c] = r;
            scratch[1 * N + r] = N - 1 - c;
            scratch[2 * N + (N - 1 - c)] = N - 1 - r;
            scratch[3 * N + (N - 1 - r)] = c;
            scratch[4 * N + (N - 1 - c)] = r;
            scratch[5 * N + c] = N - 1 - r;
            scratch[6 * N + r] = c;
            scratch[7 * N + (N - 1 - r)] = N - 1 - c;
        }
        for (int t = 1; t < 8; t++)
        {
            for (int i = 0; i < depth; i++)
            {
                int a = scratch[t * N + i];
                int b = scratch[0 * N + i];
                if (a == int.MaxValue || b == int.MaxValue)
                    continue;

                if (a < b) return false;
                if (a > b) break;
            }
        }
        return true;
    }

    private readonly record struct PartialState(
        int[] Rows, int Depth, ulong Cols, ulong D1, ulong D2);
}
