namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskSearchEngine
{
    private const ulong _deBruijn64 = 0x03F79D71B4CB0A89UL;
    private static readonly byte[] _deBruijnIndex64 = InitDeBruijn();

    private static byte[] InitDeBruijn()
    {
        var tbl = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            ulong bit = 1UL << i;
            int idx = (int)((bit * _deBruijn64) >> 58);
            tbl[idx] = (byte)i;
        }
        return tbl;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastTzcnt(ulong bit) => _deBruijnIndex64[(bit * _deBruijn64) >> 58];

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
        state._Col = 0;

        // Initialize progress reporter: 1% buckets, 1500ms heartbeat
        var reporter = new ProgressReporter(request.ReportProgress, bucketSize: 1, heartbeatMs: 1500);
        int bucketReported = -1;

        // Initial progress
        request.ReportProgress(0.0);

        ulong attacked0 = state._Cols | state._Diag1 | state._Diag2;
        state._Remaining = (~attacked0) & state._Mask;
        if (request.RestrictFirstCol)
        {
            int maxRow = (state._N + 1) / 2; if (maxRow < state._N) state._Remaining &= (1UL << maxRow) - 1UL;
        }
        if ((request.EnhancedSymmetry || request.AggressiveSymmetry) && state._N >= 14)
        {
            state._Remaining = SymmetryHelper.ApplyAdvancedSymmetryPruning(state._N, 0, state._QueenRows, state._Remaining);
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
            _N = N,
            _QueenRows = queenRows,
            _Mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL),
            _MaxRow0 = maxRow0,
            _StackFrames = new Frame[N],
            _RootTotal = request.RestrictFirstCol ? maxRow0 : N,
            _LastDepth = -1,
            _Visualize = visualize,
            _Delay = (visualize && request.DelayInMillisec > 0)
                ? Math.Max(SimulationSettings.MinDelayInMilliseconds, request.DelayInMillisec)
                : 0,
            _QueenPlacedSampleRate = sampleRate,
            _ReflectionEqual = true,
            _MinimalityEqual = true
        };
    }

    private static void MainLoop(ref SearchState s, in Request request, ProgressReporter reporter, ref int bucketReported)
    {
        int N = s._N;
        Span<int> queenRows = s._QueenRows;
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

        int col = s._Col;
        ulong cols = s._Cols;
        ulong d1 = s._Diag1;
        ulong d2 = s._Diag2;
        ulong remaining = s._Remaining;
        bool reflectionEqual = s._ReflectionEqual;
        bool minimalityEqual = s._MinimalityEqual;

        while (true)
        {
            if (request.IsCanceled()) break;

            if (col == N)
            {
                if (request.OnSolution != null)
                {
                    if (needsCopy)
                    {
                        queenRows[..N].CopyTo(solutionBuffer!);
                        if (request.OnSolution(solutionBuffer!)) break;
                    }
                    else if (request.OnSolution(s._QueenRows)) break;
                }
                col--; if (col < 0) break;
                var frame = s._StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining;
                reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                queenRows[col] = -1;
                if (s._Visualize)
                {
                    request.OnQueenPlaced(new Memory<int>(s._QueenRows));
                    if (s._Delay > 0) Thread.Sleep(s._Delay);
                }
                continue;
            }

            if (remaining == 0)
            {
                col--; if (col < 0) break;
                var frame = s._StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining;
                reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                queenRows[col] = -1;
                if (s._Visualize)
                {
                    request.OnQueenPlaced(new Memory<int>(s._QueenRows));
                    if (s._Delay > 0) Thread.Sleep(s._Delay);
                }
                continue;
            }

            ulong bit = remaining & (ulong)-(long)remaining;
            remaining &= remaining - 1;
            int row = FastTzcnt(bit);
            queenRows[col] = row;

            if (col >= pruneDepthGate &&
                SearchHelpers.ShouldPrunePrefixIncremental(
                    s._QueenRows, col, N, reflectionEnabled, prefixEnabled,
                    ref reflectionEqual, ref minimalityEqual))
            {
                queenRows[col] = -1;
                if (s._Visualize)
                {
                    request.OnQueenPlaced(new Memory<int>(s._QueenRows));
                    if (s._Delay > 0) Thread.Sleep(s._Delay);
                }
                continue;
            }

            // Root progress: use bucket/heartbeat throttling
            if (col == 0)
            {
                s._RootPlacements++;
                reporter.ReportBucket(s._RootPlacements, s._RootTotal, ref bucketReported);
            }

            if (!request.CountOnly) MaybeRaisePlacementEvent(ref s, request);
            s._StackFrames[col] = new Frame(cols, d1, d2, remaining, reflectionEqual, minimalityEqual);
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            col++;

            if (col == N) continue;

            ulong attacked = cols | d1 | d2;
            remaining = (~attacked) & s._Mask;

            if (symmetryActive)
            {
                ulong avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(
                    N, col, s._QueenRows, remaining);

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

        s._Col = col; s._Cols = cols; s._Diag1 = d1; s._Diag2 = d2; s._Remaining = remaining; s._ReflectionEqual = reflectionEqual; s._MinimalityEqual = minimalityEqual;
    }

    private static void MainLoopCountOnly(ref SearchState s, in Request request, ProgressReporter reporter, ref int bucketReported)
    {
        int N = s._N;
        int pruneDepthGate = int.MaxValue;
        bool prefixEnabled = request.PrefixMinimalityPruning;
        bool reflectionEnabled = request.ReflectionPruning;
        if (prefixEnabled || reflectionEnabled)
        {
            if (N >= 20) pruneDepthGate = 1;
            else if (N >= 16) pruneDepthGate = 2;
            else if (N >= 15) pruneDepthGate = 3;
        }

        int col = s._Col;
        ulong cols = s._Cols;
        ulong d1 = s._Diag1;
        ulong d2 = s._Diag2;
        int[] rows = s._QueenRows;
        ulong remaining = s._Remaining;
        bool reflectionEqual = s._ReflectionEqual;
        bool minimalityEqual = s._MinimalityEqual;

        while (true)
        {
            if (request.IsCanceled()) break;

            if (col == N)
            {
                if (request.OnSolution != null && request.OnSolution(rows)) break;
                col--; if (col < 0) break;
                var frame = s._StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining; reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                rows[col] = -1;
                continue;
            }

            if (remaining == 0)
            {
                col--;
                if (col < 0) break;
                var frame = s._StackFrames[col];
                cols = frame.Cols; d1 = frame.D1; d2 = frame.D2; remaining = frame.Remaining; reflectionEqual = frame.ReflectionEqual; minimalityEqual = frame.MinimalityEqual;
                rows[col] = -1;
                continue;
            }

            ulong bit = remaining & (ulong)-(long)remaining;
            remaining &= remaining - 1;
            int row = FastTzcnt(bit);
            rows[col] = row;

            if (col >= pruneDepthGate &&
                SearchHelpers.ShouldPrunePrefixIncremental(
                    rows, col, N, reflectionEnabled, prefixEnabled, ref reflectionEqual, ref minimalityEqual))
            {
                rows[col] = -1;
                continue;
            }

            // Root progress: use bucket/heartbeat throttling
            if (col == 0)
            {
                s._RootPlacements++;
                reporter.ReportBucket(s._RootPlacements, s._RootTotal, ref bucketReported);
            }

            s._StackFrames[col] = new Frame(cols, d1, d2, remaining, reflectionEqual, minimalityEqual);
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            col++;

            if (col == N) continue;

            ulong attacked = cols | d1 | d2;
            remaining = (~attacked) & s._Mask;
        }

        s._Col = col; s._Cols = cols; s._Diag1 = d1; s._Diag2 = d2; s._Remaining = remaining; s._ReflectionEqual = reflectionEqual; s._MinimalityEqual = minimalityEqual;
    }

    private static void MaybeRaisePlacementEvent(ref SearchState s, in Request request)
    {
        if (!s._Visualize) return;
        s._QueenPlacedCounter++;
        bool depthIncreased = s._Col > s._LastDepth;
        if (depthIncreased || (s._QueenPlacedCounter % s._QueenPlacedSampleRate == 0))
        {
            request.OnQueenPlaced(new Memory<int>(s._QueenRows));
            s._LastDepth = s._Col;
        }
        if (s._Delay > 0) System.Threading.Thread.Sleep(s._Delay);
    }

    private sealed class SearchState
    {
        public int _N;
        public int[] _QueenRows = [];
        public ulong _Mask;
        public ulong _Cols;
        public ulong _Diag1;
        public ulong _Diag2;
        public int _MaxRow0;
        public Frame[] _StackFrames = [];
        public int _RootPlacements;
        public int _RootTotal;
        public int _QueenPlacedCounter;
        public int _LastDepth;
        public int _Col;
        public ulong _Remaining;
        public bool _Visualize;
        public int _Delay;
        public int _QueenPlacedSampleRate;
        public bool _ReflectionEqual;
        public bool _MinimalityEqual;
    }

    private readonly record struct Frame(ulong Cols, ulong D1, ulong D2, ulong Remaining, bool ReflectionEqual, bool MinimalityEqual);
}

