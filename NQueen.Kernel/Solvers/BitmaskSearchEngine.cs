namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskSearchEngine
{
    private const ulong DeBruijn64 = 0x03F79D71B4CB0A89UL;
    private static readonly byte[] DeBruijnIndex64 = InitDeBruijn();

    private static byte[] InitDeBruijn()
    {
        var tbl = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            ulong bit = 1UL << i;
            int idx = (int)((bit * DeBruijn64) >> 58);
            tbl[idx] = (byte)i;
        }
        return tbl;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastTzcnt(ulong bit) => DeBruijnIndex64[(bit * DeBruijn64) >> 58];

    public readonly record struct Request(
        int BoardSize,
        bool RestrictFirstCol,
        bool EnhancedSymmetry,
        bool AggressiveSymmetry,
        bool CountOnly,
        DisplayMode DisplayMode,
        int DelayInMillisec,
        Guid SimulationToken,
        Func<bool> IsCanceled,
        Action<double> ReportProgress,
        Action<Memory<int>> OnQueenPlaced,
        Func<int[], bool> OnSolution,
        bool PrefixMinimalityPruning = false,
        bool ReflectionPruning = false
    );

    public static void Run(Request request) => ExecuteDepthFirst(request);

    private static void ExecuteDepthFirst(Request request)
    {
        ValidateBoardSize(request.BoardSize);
        var state = CreateState(request);
        state.Col = 0;

        // Initialize progress reporter: 1% buckets, 1500ms heartbeat
        var reporter = new ProgressReporter(request.ReportProgress, bucketSize: 1, heartbeatMs: 1500);
        int bucketReported = -1;

        // Initial progress
        request.ReportProgress(0.0);

        ulong attacked0 = state.Cols | state.Diag1 | state.Diag2;
        state.Remaining = (~attacked0) & state.Mask;
        if (request.RestrictFirstCol)
        {
            int maxRow = (state.N + 1) / 2; if (maxRow < state.N) state.Remaining &= (1UL << maxRow) - 1UL;
        }
        if ((request.EnhancedSymmetry || request.AggressiveSymmetry) && state.N >= 14)
        {
            state.Remaining = SymmetryHelper.ApplyAdvancedSymmetryPruning(state.N, 0, state.QueenRows, state.Remaining);
        }
        if (request.CountOnly) MainLoopCountOnly(ref state, request, reporter, ref bucketReported);
        else MainLoop(ref state, request, reporter, ref bucketReported);

        // Final progress
        request.ReportProgress(100.0);
    }

    private static void ValidateBoardSize(int size)
    {
        if (size > BoardSettings.MaxBitmaskBoardSize)
            throw new NotSupportedException($"Max supported board size is {BoardSettings.MaxBitmaskBoardSize}.");
    }

    private static SearchState CreateState(in Request request)
    {
        int N = request.BoardSize;
        var queenRows = new int[N];
        Array.Fill(queenRows, -1);
        int maxRow0 = request.RestrictFirstCol ? (N + 1) / 2 : N;
        bool visualize = !request.CountOnly && request.DisplayMode == DisplayMode.Visualize;
        int sampleRate = N >= SimulationSettings.QueenPlacedSamplingThresholdSize ? SimulationSettings.QueenPlacedLargeBoardSampleRate : 1;
        return new SearchState
        {
            N = N,
            QueenRows = queenRows,
            Mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL),
            MaxRow0 = maxRow0,
            StackFrames = new Frame[N],
            RootTotal = request.RestrictFirstCol ? maxRow0 : N,
            LastDepth = -1,
            Visualize = visualize,
            Delay = (visualize && request.DelayInMillisec > 0)
                ? Math.Max(SimulationSettings.MinDelayInMilliseconds, request.DelayInMillisec)
                : 0,
            QueenPlacedSampleRate = sampleRate,
            ReflectionEqual = true,
            MinimalityEqual = true
        };
    }

    private static void MainLoop(ref SearchState s, in Request request, ProgressReporter reporter, ref int bucketReported)
    {
        int N = s.N;
        Span<int> queenRows = s.QueenRows;
        int[]? solutionBuffer = null;
        bool needsCopy = request.OnSolution != null && !request.CountOnly;
        if (needsCopy) solutionBuffer = new int[N];
        bool prefixEnabled = request.PrefixMinimalityPruning;
        bool reflectionEnabled = request.ReflectionPruning;
        bool symmetryActive = (request.EnhancedSymmetry || request.AggressiveSymmetry) && N >= 14;
        bool isAggressive = request.AggressiveSymmetry && symmetryActive;

        int pruneDepthGate = int.MaxValue;
        if (prefixEnabled || reflectionEnabled)
        {
            if (N >= 20) pruneDepthGate = 1;
            else if (N >= 16) pruneDepthGate = 2;
            else if (N >= 15) pruneDepthGate = 3;
        }

        int col = s.Col;
        ulong cols = s.Cols;
        ulong d1 = s.Diag1;
        ulong d2 = s.Diag2;
        ulong remaining = s.Remaining;
        bool reflectionEqual = s.ReflectionEqual;
        bool minimalityEqual = s.MinimalityEqual;

        while (true)
        {
            if (request.IsCanceled()) break;

            if (col == N)
            {
                if (request.OnSolution != null)
                {
                    if (needsCopy)
                    {
                        queenRows.Slice(0, N).CopyTo(solutionBuffer!);
                        if (request.OnSolution(solutionBuffer!)) break;
                    }
                    else if (request.OnSolution(s.QueenRows)) break;
                }
                col--; if (col < 0) break;
                var frame = s.StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining;
                reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                queenRows[col] = -1;
                if (s.Visualize)
                {
                    request.OnQueenPlaced(new Memory<int>(s.QueenRows));
                    if (s.Delay > 0) Thread.Sleep(s.Delay);
                }
                continue;
            }

            if (remaining == 0)
            {
                col--; if (col < 0) break;
                var frame = s.StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining;
                reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                queenRows[col] = -1;
                if (s.Visualize)
                {
                    request.OnQueenPlaced(new Memory<int>(s.QueenRows));
                    if (s.Delay > 0) Thread.Sleep(s.Delay);
                }
                continue;
            }

            ulong bit = remaining & (ulong)-(long)remaining;
            remaining &= remaining - 1;
            int row = FastTzcnt(bit);
            queenRows[col] = row;

            if (col >= pruneDepthGate &&
                SearchOptimizations.ShouldPrunePrefixIncremental(
                    s.QueenRows, col, N, reflectionEnabled, prefixEnabled,
                    ref reflectionEqual, ref minimalityEqual))
            {
                queenRows[col] = -1;
                if (s.Visualize)
                {
                    request.OnQueenPlaced(new Memory<int>(s.QueenRows));
                    if (s.Delay > 0) Thread.Sleep(s.Delay);
                }
                continue;
            }

            // Root progress: use bucket/heartbeat throttling
            if (col == 0)
            {
                s.RootPlacements++;
                reporter.ReportBucket(s.RootPlacements, s.RootTotal, ref bucketReported);
            }

            if (!request.CountOnly) MaybeRaisePlacementEvent(ref s, request);
            s.StackFrames[col] = new Frame(cols, d1, d2, remaining, reflectionEqual, minimalityEqual);
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            col++;

            if (col == N) continue;

            ulong attacked = cols | d1 | d2;
            remaining = (~attacked) & s.Mask;

            if (symmetryActive)
            {
                ulong avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(
                    N, col, s.QueenRows, remaining);

                var isSecondColAggressivePrune =
                    isAggressive && col == 2 &&
                    queenRows[1] >= 0 &&
                    !(((N & 1) == 1) && queenRows[0] == N / 2);

                if (isSecondColAggressivePrune)
                {
                    int minRow = queenRows[1];
                    if (minRow < N)
                    {
                        ulong lowerMask = (1UL << minRow) - 1UL;
                        avail &= ~lowerMask;
                    }
                    else avail = 0UL;
                }
                remaining = avail;
            }
        }

        s.Col = col; s.Cols = cols; s.Diag1 = d1; s.Diag2 = d2; s.Remaining = remaining; s.ReflectionEqual = reflectionEqual; s.MinimalityEqual = minimalityEqual;
    }

    private static void MainLoopCountOnly(ref SearchState s, in Request request, ProgressReporter reporter, ref int bucketReported)
    {
        int N = s.N;
        int pruneDepthGate = int.MaxValue;
        bool prefixEnabled = request.PrefixMinimalityPruning;
        bool reflectionEnabled = request.ReflectionPruning;
        if (prefixEnabled || reflectionEnabled)
        {
            if (N >= 20) pruneDepthGate = 1;
            else if (N >= 16) pruneDepthGate = 2;
            else if (N >= 15) pruneDepthGate = 3;
        }

        int col = s.Col;
        ulong cols = s.Cols;
        ulong d1 = s.Diag1;
        ulong d2 = s.Diag2;
        int[] rows = s.QueenRows;
        ulong remaining = s.Remaining;
        bool reflectionEqual = s.ReflectionEqual;
        bool minimalityEqual = s.MinimalityEqual;

        while (true)
        {
            if (request.IsCanceled()) break;

            if (col == N)
            {
                if (request.OnSolution != null && request.OnSolution(rows)) break;
                col--; if (col < 0) break;
                var frame = s.StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining; reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                rows[col] = -1;
                continue;
            }

            if (remaining == 0)
            {
                col--;
                if (col < 0) break;
                var frame = s.StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining; reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                rows[col] = -1;
                continue;
            }

            ulong bit = remaining & (ulong)-(long)remaining;
            remaining &= remaining - 1;
            int row = FastTzcnt(bit);
            rows[col] = row;

            if (col >= pruneDepthGate &&
                Engines.SearchOptimizations.ShouldPrunePrefixIncremental(
                    rows, col, N, reflectionEnabled, prefixEnabled, ref reflectionEqual, ref minimalityEqual))
            {
                rows[col] = -1;
                continue;
            }

            // Root progress: use bucket/heartbeat throttling
            if (col == 0)
            {
                s.RootPlacements++;
                reporter.ReportBucket(s.RootPlacements, s.RootTotal, ref bucketReported);
            }

            s.StackFrames[col] = new Frame(cols, d1, d2, remaining, reflectionEqual, minimalityEqual);
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            col++;

            if (col == N) continue;

            ulong attacked = cols | d1 | d2;
            remaining = (~attacked) & s.Mask;
        }

        s.Col = col; s.Cols = cols; s.Diag1 = d1; s.Diag2 = d2; s.Remaining = remaining; s.ReflectionEqual = reflectionEqual; s.MinimalityEqual = minimalityEqual;
    }

    private static void ReportRootProgress(ref SearchState s, in Request request)
    {
        s.RootPlacements++;
        double pct = (double)s.RootPlacements / s.RootTotal * 100.0;
        request.ReportProgress(pct);
    }

    private static void MaybeRaisePlacementEvent(ref SearchState s, in Request request)
    {
        if (!s.Visualize) return;
        s.QueenPlacedCounter++;
        bool depthIncreased = s.Col > s.LastDepth;
        if (depthIncreased || (s.QueenPlacedCounter % s.QueenPlacedSampleRate == 0))
        {
            request.OnQueenPlaced(new Memory<int>(s.QueenRows));
            s.LastDepth = s.Col;
        }
        if (s.Delay > 0) System.Threading.Thread.Sleep(s.Delay);
    }

    private sealed class SearchState
    {
        public int N;
        public int[] QueenRows = [];
        public ulong Mask;
        public ulong Cols;
        public ulong Diag1;
        public ulong Diag2;
        public int MaxRow0;
        public Frame[] StackFrames = [];
        public int RootPlacements;
        public int RootTotal;
        public int QueenPlacedCounter;
        public int LastDepth;
        public int Col;
        public ulong Remaining;
        public bool Visualize;
        public int Delay;
        public int QueenPlacedSampleRate;
        public bool ReflectionEqual;
        public bool MinimalityEqual;
    }

    private readonly record struct Frame(ulong Cols, ulong D1, ulong D2, ulong Remaining, bool ReflectionEqual, bool MinimalityEqual);
}

// Simple centralized reporter with bucket/heartbeat throttling (placed in same file for convenience)
internal readonly struct ProgressReporter
{
    private readonly Action<double> _report;
    private readonly int _bucketSize;
    private readonly Stopwatch _heartbeat;
    private readonly int _heartbeatMs;

    public ProgressReporter(Action<double> report, int bucketSize = 1, int heartbeatMs = 1500)
    {
        _report = report;
        _bucketSize = bucketSize;
        _heartbeat = Stopwatch.StartNew();
        _heartbeatMs = heartbeatMs;
    }

    public void ReportBucket(int done, int totalTasks, ref int bucketReported)
    {
        double pct = totalTasks == 0 ? 100.0 : (double)done / totalTasks * 100.0;
        int bucket = (int)pct / _bucketSize * _bucketSize;
        int observed;
        while (bucket > (observed = Volatile.Read(ref bucketReported)))
        {
            if (Interlocked.CompareExchange(ref bucketReported, bucket, observed) == observed)
            {
                _report(bucket);
                break;
            }
        }
        if (_heartbeat.ElapsedMilliseconds >= _heartbeatMs)
        {
            _report(Math.Min(99.0, pct));
            _heartbeat.Restart();
        }
    }
}

