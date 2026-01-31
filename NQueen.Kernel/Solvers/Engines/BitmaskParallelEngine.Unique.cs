namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public readonly record struct UniqueRequest(
        int BoardSize,
        bool EnableEvents,
        Func<bool> ShouldMaterialize,
        Action<int[]> OnUniqueSolution,
        Action<ulong> OnCompletedUniqueCount,
        Action<double> ReportProgress
    );

    private const int PrefixPruneStartDepth = 4;
    private const int PrefixPruneThresholdN = 18;

    public static void RunUnique(UniqueRequest request)
    {
        ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);

        int N = request.BoardSize;
        request.ReportProgress(0.0);
        if (N <= 0) { request.OnCompletedUniqueCount(0); return; }

        var plan = ParallelSplitDepthHeuristic.GetSplitPlan(N);
        int splitDepth = plan.SplitDepth;

        var globalUnique = new ConcurrentDictionary<UInt128, byte>();
        int materializedCount = 0;
        int cap = request.ShouldMaterialize() ? SimulationSettings.MaxDisplayedCount : 0;
        ulong expectedTotal = N <= 29 ? ExpectedSolutionCounts.GetUniqueFast(N) : 0UL;

        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        var partialStates = new List<PartialState>(Math.Max(plan.TargetRoots, 128));
        GeneratePartialStates(N, splitDepth, mask, partialStates);

        int totalTasks = partialStates.Count;
        int progressCounter = 0;
        int progressBucketReported = -1;
        int progressBucketSize = 1;
        var lastHeartbeat = Stopwatch.StartNew();
        const int heartbeatMs = 1500;

        // NEW: cached flags for readability
        bool eventsEnabled = request.EnableEvents;
        bool hasExpectedTotal = expectedTotal > 0;

        // Replace the current Parallel.ForEach body with this overload that provides per-worker locals:
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        Parallel.ForEach(
            source: partialStates,
            parallelOptions: parallelOptions,
            localInit: () => (localUnique: new HashSet<UInt128>(), scratch: ArrayPool<int>.Shared.Rent(N * 8)),
            body: (ps, loopState, local) =>
            {
                EnumerateFromPartial(ps, local.localUnique, local.scratch);
                int done = Interlocked.Increment(ref progressCounter);
                if (eventsEnabled && !hasExpectedTotal)
                {
                    ReportBucket(done, totalTasks, ref progressBucketReported, progressBucketSize, lastHeartbeat, heartbeatMs, request);
                }
                return local;
            },
            localFinally: local =>
            {
                foreach (var k in local.localUnique) globalUnique.TryAdd(k, 0);
                ArrayPool<int>.Shared.Return(local.scratch, clearArray: false);
            });

        request.OnCompletedUniqueCount((ulong)globalUnique.Count);
        request.ReportProgress(100.0);

        // helper to centralize progress buckets/heartbeat
        static void ReportBucket(
            int done, int totalTasks,
            ref int progressBucketReported, int progressBucketSize,
            Stopwatch lastHeartbeat, int heartbeatMs,
            UniqueRequest request)
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

        // Update EnumerateFromPartial to accept the per-worker locals:
        void EnumerateFromPartial(PartialState ps, HashSet<UInt128> localUnique, int[] scratch)
        {
            int[] rowsArr = new int[N];
            Array.Fill(rowsArr, -1);
            if (ps.Rows.Length > 0)
                Array.Copy(ps.Rows, 0, rowsArr, 0, ps.Rows.Length);

            int col = ps.Depth;
            ulong cols = ps.Cols;
            ulong d1 = ps.D1;
            ulong d2 = ps.D2;
            ulong remaining = ~(cols | d1 | d2) & mask;

            ulong[] stackCols = new ulong[N];
            ulong[] stackD1 = new ulong[N];
            ulong[] stackD2 = new ulong[N];
            ulong[] stackRemaining = new ulong[N];
            while (true)
            {
                if (col == N)
                {
                    UInt128 key = SymmetryHelper.GetCanonicalKey(rowsArr, scratch, out var canonicalSpan);
                    if (localUnique.Add(key))
                    {
                        if (materializedCount < cap)
                        {
                            int newVal = Interlocked.Increment(ref materializedCount);
                            if (newVal <= cap)
                            {
                                var canonicalRows = new int[N];
                                canonicalSpan.CopyTo(canonicalRows);
                                request.OnUniqueSolution(canonicalRows);
                            }
                        }
                        if (eventsEnabled && hasExpectedTotal)
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
                    if (col < ps.Depth) break;
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

                if (N >= PrefixPruneThresholdN && col >= PrefixPruneStartDepth)
                {
                    if (!IdentityPrefixMinimal(rowsArr, col, scratch, N))
                    {
                        col--;
                        Restore(col, out remaining);
                        continue;
                    }
                }
            }

            void Restore(int c, out ulong rem)
            {
                rem = stackRemaining[c];
                cols = stackCols[c];
                d1 = stackD1[c];
                d2 = stackD2[c];
            }
        }
    }

    public static void RunUniqueUnified(
        int boardSize, bool enableEvents, int cap,
        Action<int[]> onUniqueSolution, Action<ulong> onCompletedUniqueCount,
        Action<double> reportProgress, Func<bool> capReached)
    {
        var req = new UniqueRequest(
            boardSize, enableEvents, () => cap > 0,
            onUniqueSolution, onCompletedUniqueCount, reportProgress);

        RunUnique(req);
    }

    private static void GeneratePartialStates(int N, int splitDepth, ulong mask, List<PartialState> dest)
    {
        int[] rows = new int[N];
        Array.Fill(rows, -1);
        ulong cols = 0UL, d1 = 0UL, d2 = 0UL;
        int depth = 0;

        ulong[] stackCols = new ulong[N];
        ulong[] stackD1 = new ulong[N];
        ulong[] stackD2 = new ulong[N];
        ulong[] stackAvail = new ulong[N];

        ulong avail = mask;
        while (true)
        {
            if (depth == splitDepth)
            {
                var prefix = depth > 0 ? rows[..depth].ToArray() : Array.Empty<int>();
                dest.Add(new PartialState(prefix, depth, cols, d1, d2));
                depth--;
                if (depth < 0) break;
                Restore(depth);
                continue;
            }

            if (avail == 0UL)
            {
                depth--;
                if (depth < 0) break;
                Restore(depth);
                continue;
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
            d2 = (d2 | bit) >> 1;
            depth++;

            if (depth == splitDepth)
            {
                avail = 0UL;
                continue;
            }

            avail = ~(cols | d1 | d2) & mask;

            // Apply second-column prune at split generation only
            if (depth == 1)
            {
                int firstRow = rows[0];
                if (!((N & 1) == 1 && firstRow == N / 2))
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
            d2 = stackD2[c];
            rows[c] = -1;
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
            if (r < 0) continue;

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
                if (a == int.MaxValue || b == int.MaxValue) continue;
                if (a < b) return false;
                if (a > b) break;
            }
        }
        return true;
    }

    private readonly record struct PartialState(int[] Rows, int Depth, ulong Cols, ulong D1, ulong D2);
}
