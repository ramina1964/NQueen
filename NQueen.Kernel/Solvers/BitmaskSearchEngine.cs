namespace NQueen.Kernel.Solvers.Engines;

internal sealed class BitmaskSearchEngine
{
    public readonly record struct Request(
        int BoardSize,
        bool RestrictFirstCol,
        bool EnhancedSymmetry,
        DisplayMode DisplayMode,
        int DelayInMillisec,
        Guid SimulationToken,
        Func<bool> IsCanceled,
        Action<double> ReportProgress,
        Action<Memory<int>> OnQueenPlaced,
        Func<int[], bool> OnSolution);

    public void Run(Request request) => ExecuteDepthFirst(request);

    // --- Private Implementation ---
    private void ExecuteDepthFirst(Request request)
    {
        ValidateBoardSize(request.BoardSize);
        var state = CreateState(request);
        request.ReportProgress(0.0);
        state.Remaining = ComputeAvailable(ref state, request, 0);
        MainLoop(ref state, request);
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
        bool visualize = request.DisplayMode == DisplayMode.Visualize;
        int sampleRate = N >= SimulationSettings.QueenPlacedSamplingThresholdSize
            ? SimulationSettings.QueenPlacedLargeBoardSampleRate
            : 1;

        return new SearchState
        {
            N = N,
            QueenRows = queenRows,
            Mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL),
            MaxRow0 = maxRow0,
            StackCols = new ulong[N],
            StackD1 = new ulong[N],
            StackD2 = new ulong[N],
            StackRemaining = new ulong[N],
            RootTotal = request.RestrictFirstCol ? maxRow0 : N,
            LastDepth = -1,
            Visualize = visualize,
            Delay = (visualize && request.DelayInMillisec > 0) ? request.DelayInMillisec : 0,
            QueenPlacedSampleRate = sampleRate
        };
    }

    private static void MainLoop(ref SearchState s, in Request request)
    {
        while (true)
        {
            if (request.IsCanceled()) break;

            if (s.Col == s.N)
            {
                if (request.OnSolution(s.QueenRows))
                    break;
                if (!Backtrack(ref s, out _)) break;
                continue;
            }

            if (s.Remaining == 0)
            {
                if (!Backtrack(ref s, out _)) break;
                continue;
            }

            ulong bit = ExtractLowestBit(s.Remaining);
            s.Remaining ^= bit;
            int row = BitOperations.TrailingZeroCount(bit);
            s.QueenRows[s.Col] = row;

            if (s.Col == 0)
                ReportRootProgress(ref s, request);

            MaybeRaisePlacementEvent(ref s, request);
            PushState(ref s, bit);
            s.Col++;
            if (s.Col == s.N) continue;
            s.Remaining = ComputeAvailable(ref s, request, s.Col);
        }
    }

    private static bool Backtrack(ref SearchState s, out ulong remaining)
    {
        s.Col--;
        if (s.Col < 0)
        {
            remaining = 0;
            return false;
        }
        s.Cols = s.StackCols[s.Col];
        s.Diag1 = s.StackD1[s.Col];
        s.Diag2 = s.StackD2[s.Col];
        remaining = s.StackRemaining[s.Col];
        s.Remaining = remaining;
        return true;
    }

    private static void PushState(ref SearchState s, ulong bit)
    {
        s.StackCols[s.Col] = s.Cols;
        s.StackD1[s.Col] = s.Diag1;
        s.StackD2[s.Col] = s.Diag2;
        s.StackRemaining[s.Col] = s.Remaining;
        s.Cols |= bit;
        s.Diag1 = (s.Diag1 | bit) << 1;
        s.Diag2 = (s.Diag2 | bit) >> 1;
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
        if (s.Delay > 0)
            Thread.Sleep(s.Delay);
    }

    private static ulong ComputeAvailable(ref SearchState s, in Request request, int col)
    {
        ulong avail = ~(s.Cols | s.Diag1 | s.Diag2) & s.Mask;
        int maxRow = s.N;
        // Disable aggressive symmetry pruning for small N (N <= 8)
        if (s.N > 8)
        {
            int splitDepth = s.RootTotal > 0 ? s.RootTotal : 1;
            if (request.RestrictFirstCol && request.EnhancedSymmetry && col < splitDepth)
            {
                maxRow = (s.N + 1) / 2;
                if ((s.N & 1) == 1 && col == 0 && s.QueenRows[0] == s.N / 2)
                    maxRow = s.N / 2;
            }
        }
        if (maxRow < s.N)
            avail &= (1UL << maxRow) - 1UL;
        return avail;
    }

    private static ulong ExtractLowestBit(ulong v) => v & (ulong)-(long)v;

    // --- Private state container ---
    private sealed class SearchState
    {
        public int N;
        public int[] QueenRows = Array.Empty<int>();
        public ulong Mask;
        public ulong Cols;
        public ulong Diag1;
        public ulong Diag2;
        public int MaxRow0;
        public ulong[] StackCols = Array.Empty<ulong>();
        public ulong[] StackD1 = Array.Empty<ulong>();
        public ulong[] StackD2 = Array.Empty<ulong>();
        public ulong[] StackRemaining = Array.Empty<ulong>();
        public int RootPlacements;
        public int RootTotal;
        public int QueenPlacedCounter;
        public int LastDepth;
        public int Col;
        public ulong Remaining;
        public bool Visualize;
        public int Delay;
        public int QueenPlacedSampleRate;
    }
}
