namespace NQueen.Kernel.Solvers.Engines;

using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Tasks;
using NQueen.Domain.Settings;

internal sealed partial class BitmaskParallelEngine
{
    public static void RunAll(AllRequest request)
    {
        int N = request.BoardSize;
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
        rootStack.Push(new RootFrame(0, 0UL, 0UL, 0UL, new int[N]));
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
        int EstimateWeight(in RootFrame f)
        {
            int depth = f.Col; ulong cols = f.Cols; ulong d1 = f.D1; ulong d2 = f.D2; int steps = Math.Min(SimulationSettings.WeightLookaheadDepth, N - depth); long w = 1;
            for (int i = 0; i < steps; i++)
            {
                ulong avail = ~(cols | d1 | d2) & mask; int bc = BitOperations.PopCount(avail);
                if (bc == 0) break; w *= bc; ulong bit = avail & (ulong)-(long)avail; cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1;
            }
            if (w > int.MaxValue) w = int.MaxValue; return (int)w;
        }
        rootList.Sort((a, b) => EstimateWeight(b).CompareTo(EstimateWeight(a)));

        int totalRoots = rootList.Count; int rootsCompleted = 0; int lastPercentReported = -1;
        bool throttle = N >= SimulationSettings.LargeBoardProgressThrottleThreshold;
        int bucketSize = SimulationSettings.ProgressThresholdPct; if (bucketSize < 1) bucketSize = 1;
        int globalMaterialized = 0; int cap = request.MaterializeCap;

        var workStack = new ConcurrentStack<RootFrame>(rootList);
        int workerCount = logicalCores;
        var tasks = new List<Task<ulong>>(workerCount);
        for (int w = 0; w < workerCount; w++)
        {
            tasks.Add(Task.Run(() =>
            {
                ulong localTotal = 0;
                while (workStack.TryPop(out var root))
                {
                    var rowsArr = root.Rows; int startCol = root.Col;
                    for (int i = startCol; i < N; i++) if (rowsArr[i] == 0) rowsArr[i] = -1;
                    ulong cols = root.Cols; ulong d1 = root.D1; ulong d2 = root.D2;
                    ulong[] stackCols = new ulong[N]; ulong[] stackD1 = new ulong[N]; ulong[] stackD2 = new ulong[N]; ulong[] stackRemaining = new ulong[N];
                    int col = startCol; ulong remaining = ~(cols | d1 | d2) & mask;
                    while (true)
                    {
                        if (col == N)
                        {
                            localTotal++;
                            if (globalMaterialized < cap)
                            {
                                int mat = Interlocked.Increment(ref globalMaterialized);
                                if (mat <= cap)
                                {
                                    request.OnSolution((int[])rowsArr.Clone());
                                }
                            }
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col]; continue;
                        }
                        if (remaining == 0)
                        {
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col]; continue;
                        }
                        ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit; int row = BitOperations.TrailingZeroCount(bit);
                        rowsArr[col] = row; stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                        cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1; col++; if (col == N) continue; remaining = ~(cols | d1 | d2) & mask;
                    }
                    if (request.EnableEvents)
                    {
                        int done = Interlocked.Increment(ref rootsCompleted);
                        ReportRootProgress(done, totalRoots, throttle, bucketSize, ref lastPercentReported, request.ReportProgress);
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
        int N = boardSize;
        if (splitDepth < 1) splitDepth = 1;
        if (splitDepth > N) splitDepth = N;
        reportProgress(0.0);
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        int logicalCores = Environment.ProcessorCount;
        int minRootsTarget = logicalCores * SimulationSettings.AdaptiveRootMultiplier;
        int branchThresholdConst = SimulationSettings.RootBranchThreshold;
        var rootStack = new Stack<RootFrame>();
        rootStack.Push(new RootFrame(0, 0UL, 0UL, 0UL, new int[N]));
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
        int EstimateWeight(in RootFrame f)
        {
            int depth = f.Col; ulong cols = f.Cols; ulong d1 = f.D1; ulong d2 = f.D2; int steps = Math.Min(SimulationSettings.WeightLookaheadDepth, N - depth); long w = 1;
            for (int i = 0; i < steps; i++)
            {
                ulong avail = ~(cols | d1 | d2) & mask; int bc = BitOperations.PopCount(avail);
                if (bc == 0) break; w *= bc; ulong bit = avail & (ulong)-(long)avail; cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1;
            }
            if (w > int.MaxValue) w = int.MaxValue; return (int)w;
        }
        rootList.Sort((a, b) => EstimateWeight(b).CompareTo(EstimateWeight(a)));

        int totalRoots = rootList.Count; int rootsCompleted = 0; int lastPercentReported = -1;
        bool throttle = N >= SimulationSettings.LargeBoardProgressThrottleThreshold;
        int bucketSize = SimulationSettings.ProgressThresholdPct; if (bucketSize < 1) bucketSize = 1;
        int globalMaterialized = 0;
        var workStack = new ConcurrentStack<RootFrame>(rootList);
        int workerCount = logicalCores;
        var tasks = new List<Task<ulong>>(workerCount);
        for (int w = 0; w < workerCount; w++)
        {
            tasks.Add(Task.Run(() =>
            {
                ulong localTotal = 0;
                while (workStack.TryPop(out var root))
                {
                    var rowsArr = root.Rows; int startCol = root.Col;
                    for (int i = startCol; i < N; i++) if (rowsArr[i] == 0) rowsArr[i] = -1;
                    ulong cols = root.Cols; ulong d1 = root.D1; ulong d2 = root.D2;
                    ulong[] stackCols = new ulong[N]; ulong[] stackD1 = new ulong[N]; ulong[] stackD2 = new ulong[N]; ulong[] stackRemaining = new ulong[N];
                    int col = startCol; ulong remaining = ~(cols | d1 | d2) & mask;
                    while (true)
                    {
                        if (col == N)
                        {
                            localTotal++;
                            int mat = Interlocked.Increment(ref globalMaterialized);
                            if (mat <= cap)
                            {
                                onSolution((int[])rowsArr.Clone());
                            }
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col]; continue;
                        }
                        if (remaining == 0)
                        {
                            col--; if (col < startCol) break; cols = stackCols[col]; d1 = stackD1[col]; d2 = stackD2[col]; remaining = stackRemaining[col]; continue;
                        }
                        ulong bit = remaining & (ulong)-(long)remaining; remaining ^= bit; int row = BitOperations.TrailingZeroCount(bit);
                        rowsArr[col] = row; stackCols[col] = cols; stackD1[col] = d1; stackD2[col] = d2; stackRemaining[col] = remaining;
                        cols |= bit; d1 = (d1 | bit) << 1; d2 = (d2 | bit) >> 1; col++; if (col == N) continue; remaining = ~(cols | d1 | d2) & mask;
                    }
                    if (enableEvents)
                    {
                        int done = Interlocked.Increment(ref rootsCompleted);
                        ReportRootProgress(done, totalRoots, throttle, bucketSize, ref lastPercentReported, reportProgress);
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
