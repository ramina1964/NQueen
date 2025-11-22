using System;
namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskSearchEngine
{
    public readonly record struct Request(
        int BoardSize,
        bool RestrictFirstCol,
        bool EnhancedSymmetry,
        bool AggressiveSymmetry,
        bool CountOnly, // new: enables fast path (no solution cloning, no placement events)
        DisplayMode DisplayMode,
        int DelayInMillisec,
        Guid SimulationToken,
        Func<bool> IsCanceled,
        Action<double> ReportProgress,
        Action<Memory<int>> OnQueenPlaced,
        Func<int[], bool> OnSolution
    );

    public static void Run(Request request) => ExecuteDepthFirst(request);

    // --- Private Implementation ---
    private static void ExecuteDepthFirst(Request request)
    {
        ValidateBoardSize(request.BoardSize);
        var state = CreateState(request);
        state.Col = 0;
        request.ReportProgress(0.0);
        ulong attacked0 = state.Cols | state.Diag1 | state.Diag2;
        state.Remaining = (~attacked0) & state.Mask;
        if (request.RestrictFirstCol)
        {
            int maxRow = (state.N + 1) / 2; // include center for odd N
            if (maxRow < state.N)
                state.Remaining &= (1UL << maxRow) - 1UL;
        }
        if ((request.EnhancedSymmetry || request.AggressiveSymmetry) && state.N >= 14)
        {
            state.Remaining = SymmetryHelper.ApplyAdvancedSymmetryPruning(state.N, 0, state.QueenRows, state.Remaining);
        }
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
        bool visualize = !request.CountOnly && request.DisplayMode == DisplayMode.Visualize;
        int sampleRate = N >= SimulationSettings.QueenPlacedSamplingThresholdSize ? SimulationSettings.QueenPlacedLargeBoardSampleRate : 1;
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
        int N = s.N;
        Span<int> queenRows = s.QueenRows; // local span for fast indexing
        int[]? solutionBuffer = null;
        bool needsCopy = request.OnSolution != null && !request.CountOnly;
        if (needsCopy) solutionBuffer = new int[N];
        bool prefixEnabled = SearchOptimizations.PrefixMinimalityPruningEnabled;
        bool reflectionEnabled = SearchOptimizations.ReflectionPrefixPruningEnabled;
        bool incrementalEnabled = SearchOptimizations.IncrementalCanonicalizationEnabled && !request.CountOnly;
        int pruneDepthGate = int.MaxValue;
        if (prefixEnabled || reflectionEnabled)
        {
            if (N >= 20) pruneDepthGate = 1;
            else if (N >= 16) pruneDepthGate = 2;
            else if (N >= 15) pruneDepthGate = 3;
        }
        int[]? incScratch = null;
        if (incrementalEnabled && N > 0)
        {
            incScratch = s.IncrementalScratch ??= new int[N * 8];
            Array.Fill(incScratch, -1);
        }
        ulong localCols = s.Cols;
        ulong localD1 = s.Diag1;
        ulong localD2 = s.Diag2;
        while (true)
        {
            if (request.IsCanceled()) break;
            if (s.Col == N)
            {
                if (request.OnSolution != null)
                {
                    if (needsCopy)
                    {
                        queenRows.Slice(0, N).CopyTo(solutionBuffer!);
                        if (request.OnSolution(solutionBuffer!)) break;
                    }
                    else
                    {
                        if (request.OnSolution(s.QueenRows)) break;
                    }
                }
                if (!BacktrackInline(ref s)) break;
                localCols = s.Cols; localD1 = s.Diag1; localD2 = s.Diag2;
                continue;
            }
            if (s.Remaining == 0)
            {
                if (!BacktrackInline(ref s)) break;
                localCols = s.Cols; localD1 = s.Diag1; localD2 = s.Diag2;
                continue;
            }
            ulong remainingLocal = s.Remaining;
            ulong bit = remainingLocal & (ulong)-(long)remainingLocal;
            remainingLocal &= remainingLocal - 1;
            s.Remaining = remainingLocal;
            int row = BitOperations.TrailingZeroCount(bit);
            queenRows[s.Col] = row;
            if (s.Col >= pruneDepthGate && ShouldPrunePrefixFast(s.QueenRows, s.Col, N, reflectionEnabled, prefixEnabled))
            {
                queenRows[s.Col] = -1;
                continue;
            }
            if (incrementalEnabled && incScratch != null)
            {
                int col = s.Col;
                incScratch[0 * N + col] = row;
                incScratch[5 * N + col] = N - 1 - row;
            }
            if (s.Col == 0) ReportRootProgress(ref s, request);
            if (!request.CountOnly) MaybeRaisePlacementEvent(ref s, request);
            PushState(ref s, bit);
            localCols |= bit;
            localD1 = (localD1 | bit) << 1;
            localD2 = (localD2 | bit) >> 1;
            s.Col++;
            if (s.Col == N) continue;
            ulong attacked = localCols | localD1 | localD2;
            ulong avail = (~attacked) & s.Mask;
            if ((request.EnhancedSymmetry || request.AggressiveSymmetry) && N >= 14)
            {
                avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(N, s.Col, s.QueenRows, avail);
                if (request.AggressiveSymmetry && s.Col == 2 && queenRows[1] >= 0 && !((N & 1) == 1 && queenRows[0] == N / 2))
                {
                    int minRow = queenRows[1];
                    if (minRow < N)
                    {
                        ulong lowerMask = (1UL << minRow) - 1UL;
                        avail &= ~lowerMask;
                    }
                    else avail = 0UL;
                }
            }
            s.Remaining = avail;
        }
    }

    private static bool ShouldPrunePrefixFast(int[] rows, int depth, int N, bool reflectionEnabled, bool minimalityEnabled)
    {
        if (!reflectionEnabled && !minimalityEnabled) return false;
        if (reflectionEnabled)
        {
            for (int i = 0; i <= depth; i++)
            {
                int r = rows[i]; if (r < 0) return false;
                int reflected = N - 1 - r;
                if (r > reflected) return true;
                if (r < reflected) break;
            }
        }
        if (!minimalityEnabled) return false;
        for (int i = 0; i <= depth; i++)
        {
            int a = rows[i]; if (a < 0) return false;
            int b = rows[depth - i]; if (b < 0) return false;
            int transformed = N - 1 - b;
            if (a > transformed) return true;
            if (a < transformed) break;
        }
        return false;
    }

    private static bool BacktrackInline(ref SearchState s)
    {
        s.Col--;
        if (s.Col < 0)
        {
            s.Remaining = 0UL;
            return false;
        }
        s.Cols = s.StackCols[s.Col];
        s.Diag1 = s.StackD1[s.Col];
        s.Diag2 = s.StackD2[s.Col];
        s.Remaining = s.StackRemaining[s.Col];
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
        public int[]? IncrementalScratch;
    }
}
// end of file
