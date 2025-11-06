namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskSearchEngine
{
    public readonly record struct Request(
        int BoardSize,
        bool RestrictFirstCol,
        bool EnhancedSymmetry,
        bool AggressiveSymmetry,
        DisplayMode DisplayMode,
        int DelayInMillisec,
        Guid SimulationToken,
        Func<bool> IsCanceled,
        Action<double> ReportProgress,
        Action<Memory<int>> OnQueenPlaced,
        Func<int[], bool> OnSolution // Removed optional span-based callback (unused)
    );

    public static void Run(Request request) => ExecuteDepthFirst(request);

    // --- Private Implementation ---
    private static void ExecuteDepthFirst(Request request)
    {
        ValidateBoardSize(request.BoardSize);
        var state = CreateState(request);
        state.Col =0; // Ensure search always starts from the first column
        request.ReportProgress(0.0);
        // Use the new inlined ComputeAvailableInline
        state.Remaining = ComputeAvailableInline(ref state, request,0, state.Mask, state.N);
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
        // Hoist frequently used fields
        int N = s.N;
        ulong mask = s.Mask;
        int[] solutionBuffer = null!;
        bool needsCopy = request.OnSolution != null;
        if (needsCopy) solutionBuffer = new int[N];
        while (true)
        {
            if (request.IsCanceled()) break;

            if (s.Col == N)
            {
                bool valid = true;
                for (int i =0; i < N; i++)
                {
                    if (s.QueenRows[i] <0)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid && request.OnSolution != null)
                {
                    // Only copy if callback might mutate or store the array
                    if (needsCopy)
                    {
                        Buffer.BlockCopy(s.QueenRows,0, solutionBuffer,0, N * sizeof(int));
                        if (request.OnSolution(solutionBuffer))
                            break;
                    }
                    else
                    {
                        if (request.OnSolution(s.QueenRows))
                            break;
                    }
                }
                if (!BacktrackInline(ref s)) break;
                continue;
            }

            if (s.Remaining ==0)
            {
                if (!BacktrackInline(ref s)) break;
                continue;
            }

            // Inline ExtractLowestBit
            ulong bit = s.Remaining & (ulong)-(long)s.Remaining;
            s.Remaining ^= bit;
            int row = BitOperations.TrailingZeroCount(bit);
            s.QueenRows[s.Col] = row;

            if (s.Col ==0)
                ReportRootProgress(ref s, request);

            MaybeRaisePlacementEvent(ref s, request);
            PushState(ref s, bit);
            s.Col++;
            if (s.Col == N) continue;
            s.Remaining = ComputeAvailableInline(ref s, request, s.Col, mask, N);
        }
    }

    // Inline Backtrack logic
    private static bool BacktrackInline(ref SearchState s)
    {
        s.Col--;
        if (s.Col <0)
        {
            s.Remaining =0;
            return false;
        }
        s.Cols = s.StackCols[s.Col];
        s.Diag1 = s.StackD1[s.Col];
        s.Diag2 = s.StackD2[s.Col];
        s.Remaining = s.StackRemaining[s.Col];
        return true;
    }

    // Combined/flattened ComputeAvailable
    private static ulong ComputeAvailableInline(ref SearchState s, in Request request, int col, ulong mask, int N)
    {
        ulong avail = ~(s.Cols | s.Diag1 | s.Diag2) & mask;
        if (N <=8) return avail;
        int maxRow = N;
        int splitDepth = s.RootTotal >0 ? s.RootTotal :1;
        if (request.RestrictFirstCol && request.EnhancedSymmetry && col < splitDepth)
        {
            maxRow = (N +1) /2;
            if ((N &1) ==1 && col ==0 && s.QueenRows[0] == N /2)
                maxRow = N /2;
        }
        if (maxRow < N)
            avail &= (1UL << maxRow) -1UL;
        // Unified symmetry pruning
        if ((request.EnhancedSymmetry || request.AggressiveSymmetry) && N >8)
        {
            avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(N, col, s.QueenRows, avail);
            if (request.AggressiveSymmetry && col ==2 && s.QueenRows[1] >=0 && !((N &1) ==1 && s.QueenRows[0] == N /2))
            {
                int minRow = s.QueenRows[1];
                if (minRow < N)
                {
                    ulong lowerMask = (1UL << minRow) -1UL;
                    avail &= ~lowerMask;
                }
                else
                {
                    avail =0UL;
                }
            }
        }
        return avail;
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

    // --- Private state container ---
    private sealed class SearchState
    {
        public int N;
        public int[] QueenRows = [];
        public ulong Mask;
        public ulong Cols;
        public ulong Diag1;
        public ulong Diag2;
        public int MaxRow0;
        public ulong[] StackCols = [];
        public ulong[] StackD1 = [];
        public ulong[] StackD2 = [];
        public ulong[] StackRemaining = [];
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
