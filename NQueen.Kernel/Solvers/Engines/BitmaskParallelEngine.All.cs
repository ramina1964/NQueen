namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public static void RunAll(AllRequest request)
    {
        int N = request.BoardSize;
        ulong expectedTotal = N <= 29 ? ExpectedSolutionCounts.GetAllFast(N) : 0UL;
        int splitDepth = request.RootSplitDepth < 1 ? 1 : request.RootSplitDepth;
        if (splitDepth > N) splitDepth = N;
        if (request.RootSplitDepth == -1)
            splitDepth = ParallelSplitDepthHeuristic.GetOptimalSplitDepth(N);
        request.ReportProgress(0.0);
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int logicalCores = Environment.ProcessorCount;
        int minRootsTarget = logicalCores * SimulationSettings.AdaptiveRootMultiplier;
        int branchThresholdConst = SimulationSettings.RootBranchThreshold;
        var rootStack = new Stack<RootFrame>();
        int[] init = new int[N]; Array.Fill(init, -1);
        rootStack.Push(new RootFrame(0, 0UL, 0UL, 0UL, init));
        var rootList = new List<RootFrame>(minRootsTarget);
        while (rootStack.Count > 0)
        {
            var frame = rootStack.Pop();
            if (frame.Col == splitDepth || rootList.Count >= minRootsTarget)
            { rootList.Add(frame); continue; }
            ulong avail = ~(frame.Cols | frame.D1 | frame.D2) & mask;
            int branchCount = BitOperations.PopCount(avail);
            if (branchCount <= 1 && frame.Col > 0)
            { rootList.Add(frame); continue; }
            while (avail != 0)
            {
                ulong bit = avail & (ulong)-(long)avail; avail ^= bit; int row = BitOperations.TrailingZeroCount(bit);
                var rowsCopy = (int[])frame.Rows.Clone(); rowsCopy[frame.Col] = row;
                ulong cols = frame.Cols | bit; ulong d1 = (frame.D1 | bit) << 1; ulong d2 = (frame.D2 | bit) >> 1; int nextDepth = frame.Col + 1;
                if (nextDepth >= splitDepth)
                { rootList.Add(new RootFrame(nextDepth, cols, d1, d2, rowsCopy)); }
                else
                {
                    ulong nextAvail = ~(cols | d1 | d2) & mask; int nextBranch = BitOperations.PopCount(nextAvail);
                    if (rootList.Count >= minRootsTarget || nextBranch <= branchThresholdConst)
                        rootList.Add(new RootFrame(nextDepth, cols, d1, d2, rowsCopy));
                    else
                        rootStack.Push(new RootFrame(nextDepth, cols, d1, d2, rowsCopy));
                }
            }
        }
        int totalRoots = rootList.Count; int rootsCompleted = 0; int lastPercentReported = -1;
        bool throttle = N >= SimulationSettings.LargeBoardProgressThrottleThreshold;
        int bucketSize = SimulationSettings.ProgressThresholdPct; if (bucketSize < 1) bucketSize = 1;
        int globalMaterialized = 0; int cap = request.MaterializeCap;
        long globalCountSoFar = 0; // actual solutions counted (atomic)
        var workStack = new ConcurrentStack<RootFrame>(rootList);
        int workerCount = logicalCores;
        var tasks = new List<Task<ulong>>(workerCount);
        var lastHeartbeat = Stopwatch.StartNew();
        const int heartbeatMs = 1500; // supply periodic progress even if buckets unchanged
        for (int w = 0; w < workerCount; w++)
        {
            tasks.Add(Task.Run(() =>
            {
                ulong localTotal = 0;
                while (workStack.TryPop(out var root))
                {
                    var rowsArr = (int[])root.Rows.Clone();
                    int startCol = root.Col;
                    ulong cols = root.Cols; ulong d1 = root.D1; ulong d2 = root.D2;
                    ulong[] stackCols = new ulong[N]; ulong[] stackD1 = new ulong[N]; ulong[] stackD2 = new ulong[N]; ulong[] stackRemaining = new ulong[N];
                    int col = startCol; ulong remaining = ~(cols | d1 | d2) & mask;
                    while (true)
                    {
                        if (col == N)
                        {
                            localTotal++;
                            long newGlobal = Interlocked.Increment(ref globalCountSoFar);
                            if (globalMaterialized < cap)
                            {
                                int mat = Interlocked.Increment(ref globalMaterialized);
                                if (mat <= cap)
                                {
                                    request.OnSolution((int[])rowsArr.Clone());
                                }
                            }
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col];
                            // Fine-grained progress based on counted solutions when expectedTotal known
                            if (request.EnableEvents && expectedTotal > 0)
                            {
                                double pctSol = (double)newGlobal / expectedTotal * 100.0;
                                if (pctSol > 99.0) pctSol = 99.0; // reserve 100% for completion
                                // Heartbeat ensures occasional update
                                if (lastHeartbeat.ElapsedMilliseconds >= heartbeatMs || pctSol >= lastPercentReported + bucketSize)
                                {
                                    int bucket = expectedTotal > 0 ? (int)pctSol : (int)pctSol;
                                    int observed = Volatile.Read(ref lastPercentReported);
                                    if (bucket > observed && Interlocked.CompareExchange(ref lastPercentReported, bucket, observed) == observed)
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
                            continue;
                        }
                        if (remaining == 0)
                        {
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col]; continue;
                        }
                        ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit; int row = BitOperations.TrailingZeroCount(bit);
                        rowsArr[col] = row; stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                        cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1; col++; if (col == N) continue; remaining = ~(cols | d1 | d2) & mask;
                    }
                    if (request.EnableEvents && expectedTotal == 0)
                    {
                        // Fallback: root-based progress when expected total unknown
                        int doneRoots = Interlocked.Increment(ref rootsCompleted);
                        ReportRootProgress(doneRoots, totalRoots, throttle, bucketSize, ref lastPercentReported, request.ReportProgress);
                    }
                }
                return localTotal;
            }));
        }
        Task.WaitAll(tasks.ToArray());
        ulong totalCount = 0; foreach (var t in tasks) totalCount += t.Result;
        request.OnCompleted(totalCount);
        request.ReportProgress(100.0);
    }

    public static void RunAllUnified(
        int boardSize,
        int splitDepth,
        bool enableEvents,
        int cap,
        Action<int[]> onSolution,
        Action<ulong> onCompleted,
        Action<double> reportProgress,
        Func<bool> capReached)
    {
        // Disable visualization events for parallel path if visualize mode expected.
        // The caller now routes visualization to sequential engine; ensure no queen placement mutation side-effects here.
        enableEvents = false;

        int N = boardSize;
        ulong expectedTotal = N <= 29 ? ExpectedSolutionCounts.GetAllFast(N) : 0UL;
        if (splitDepth < 1) splitDepth = 1;
        if (splitDepth > N) splitDepth = N;
        reportProgress(0.0);
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int logicalCores = Environment.ProcessorCount;
        int minRootsTarget = logicalCores * SimulationSettings.AdaptiveRootMultiplier;
        int branchThresholdConst = SimulationSettings.RootBranchThreshold;
        var rootStack = new Stack<RootFrame>();
        int[] init = new int[N]; Array.Fill(init, -1);
        rootStack.Push(new RootFrame(0, 0UL, 0UL, 0UL, init));
        var rootList = new List<RootFrame>(minRootsTarget);
        while (rootStack.Count > 0)
        {
            var frame = rootStack.Pop();
            if (frame.Col == splitDepth || rootList.Count >= minRootsTarget)
            { rootList.Add(frame); continue; }
            ulong avail = ~(frame.Cols | frame.D1 | frame.D2) & mask;
            int branchCount = BitOperations.PopCount(avail);
            if (branchCount <= 1 && frame.Col > 0)
            { rootList.Add(frame); continue; }
            while (avail != 0)
            {
                ulong bit = avail & (ulong)-(long)avail; avail ^= bit; int row = BitOperations.TrailingZeroCount(bit);
                var rowsCopy = (int[])frame.Rows.Clone(); rowsCopy[frame.Col] = row;
                ulong cols = frame.Cols | bit; ulong d1 = (frame.D1 | bit) << 1; ulong d2 = (frame.D2 | bit) >> 1; int nextDepth = frame.Col + 1;
                if (nextDepth >= splitDepth)
                { rootList.Add(new RootFrame(nextDepth, cols, d1, d2, rowsCopy)); }
                else
                {
                    ulong nextAvail = ~(cols | d1 | d2) & mask; int nextBranch = BitOperations.PopCount(nextAvail);
                    if (rootList.Count >= minRootsTarget || nextBranch <= branchThresholdConst)
                        rootList.Add(new RootFrame(nextDepth, cols, d1, d2, rowsCopy));
                    else
                        rootStack.Push(new RootFrame(nextDepth, cols, d1, d2, rowsCopy));
                }
            }
        }
        int totalRoots = rootList.Count; int rootsCompleted = 0; int lastPercentReported = -1;
        bool throttle = N >= SimulationSettings.LargeBoardProgressThrottleThreshold;
        int bucketSize = SimulationSettings.ProgressThresholdPct; if (bucketSize < 1) bucketSize = 1;
        int globalMaterialized = 0;
        long globalCountSoFar = 0;
        var workStack = new ConcurrentStack<RootFrame>(rootList);
        int workerCount = logicalCores;
        var tasks = new List<Task<ulong>>(workerCount);
        var lastHeartbeat = Stopwatch.StartNew();
        const int heartbeatMs = 1500;
        for (int w = 0; w < workerCount; w++)
        {
            tasks.Add(Task.Run(() =>
            {
                ulong localTotal = 0;
                while (workStack.TryPop(out var root))
                {
                    var rowsArr = (int[])root.Rows.Clone();
                    int startCol = root.Col;
                    ulong cols = root.Cols; ulong d1 = root.D1; ulong d2 = root.D2;
                    ulong[] stackCols = new ulong[N]; ulong[] stackD1 = new ulong[N]; ulong[] stackD2 = new ulong[N]; ulong[] stackRemaining = new ulong[N];
                    int col = startCol; ulong remaining = ~(cols | d1 | d2) & mask;
                    while (true)
                    {
                        if (col == N)
                        {
                            localTotal++;
                            long newGlobal = Interlocked.Increment(ref globalCountSoFar);
                            int mat = Interlocked.Increment(ref globalMaterialized);
                            if (mat <= cap)
                            {
                                onSolution((int[])rowsArr.Clone());
                            }
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col];
                            if (enableEvents && expectedTotal > 0)
                            {
                                double pctSol = (double)newGlobal / expectedTotal * 100.0;
                                if (pctSol > 99.0) pctSol = 99.0;
                                if (lastHeartbeat.ElapsedMilliseconds >= heartbeatMs || pctSol >= lastPercentReported + bucketSize)
                                {
                                    int bucket = (int)pctSol;
                                    int observed = Volatile.Read(ref lastPercentReported);
                                    if (bucket > observed && Interlocked.CompareExchange(ref lastPercentReported, bucket, observed) == observed)
                                    {
                                        reportProgress(bucket);
                                        lastHeartbeat.Restart();
                                    }
                                    else if (lastHeartbeat.ElapsedMilliseconds >= heartbeatMs)
                                    {
                                        reportProgress(Math.Min(99.0, pctSol));
                                        lastHeartbeat.Restart();
                                    }
                                }
                            }
                            continue;
                        }
                        if (remaining == 0)
                        {
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col]; continue;
                        }
                        ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit; int row = BitOperations.TrailingZeroCount(bit);
                        rowsArr[col] = row; stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                        cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1; col++; if (col == N) continue; remaining = ~(cols | d1 | d2) & mask;
                    }
                    if (enableEvents && expectedTotal == 0)
                    {
                        int doneRoots = Interlocked.Increment(ref rootsCompleted);
                        ReportRootProgress(doneRoots, totalRoots, throttle, bucketSize, ref lastPercentReported, reportProgress);
                    }
                }
                return localTotal;
            }));
        }
        Task.WaitAll(tasks.ToArray());
        ulong totalCount = 0; foreach (var t in tasks) totalCount += t.Result;
        onCompleted(totalCount);
        reportProgress(100.0);
    }
}
