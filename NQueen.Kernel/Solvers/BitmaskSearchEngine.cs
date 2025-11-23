using System;
namespace NQueen.Kernel.Solvers;

internal sealed class BitmaskSearchEngine
{
    // Correct De Bruijn sequence implementation for 64-bit trailing zero count.
    // We build the index table at runtime to avoid hard-coded permutation errors.
    private const ulong DeBruijn64 = 0x03F79D71B4CB0A89UL;
    private static readonly byte[] DeBruijnIndex64 = InitDeBruijn();
#if DEBUG
    private static readonly bool _deBruijnVerified = VerifyDeBruijn();
#endif
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
#if DEBUG
    private static bool VerifyDeBruijn()
    {
        // Ensure all slots filled uniquely.
        var seen = new bool[64];
        for (int i = 0; i < 64; i++)
        {
            int v = DeBruijnIndex64[i];
            if (v < 0 || v > 63) throw new Exception("Invalid DeBruijn index value.");
            if (seen[v]) throw new Exception("Duplicate DeBruijn mapping detected.");
            seen[v] = true;
        }
        for (int i = 0; i < 64; i++)
        {
            ulong bit = 1UL << i;
            int fast = DeBruijnIndex64[(bit * DeBruijn64) >> 58];
            int intrinsic = System.Numerics.BitOperations.TrailingZeroCount(bit);
            if (fast != intrinsic) throw new Exception("DeBruijn mapping mismatch.");
        }
        return true;
    }
#endif
    private static int FastTzcnt(ulong bit) => DeBruijnIndex64[(bit * DeBruijn64) >> 58];

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
        if (request.CountOnly)
        {
            MainLoopCountOnly(ref state, request);
        }
        else
        {
            MainLoop(ref state, request);
        }
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

    // Existing full-feature loop retained for materialization paths
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
        // Hoisted symmetry flags
        bool symmetryActive = (request.EnhancedSymmetry || request.AggressiveSymmetry) && N >= 14;
        bool aggressiveActive = request.AggressiveSymmetry && symmetryActive;
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
        // Local copies of search state to minimize field accesses
        int col = s.Col;
        ulong cols = s.Cols;
        ulong d1 = s.Diag1;
        ulong d2 = s.Diag2;
        ulong remaining = s.Remaining;
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
                    else
                    {
                        if (request.OnSolution(s.QueenRows)) break; // using original array reference
                    }
                }
                // backtrack
                col--;
                if (col < 0) break;
                cols = s.StackCols[col];
                d1   = s.StackD1[col];
                d2   = s.StackD2[col];
                remaining = s.StackRemaining[col];
                queenRows[col] = -1;
                continue;
            }
            if (remaining == 0)
            {
                col--;
                if (col < 0) break;
                cols = s.StackCols[col];
                d1   = s.StackD1[col];
                d2   = s.StackD2[col];
                remaining = s.StackRemaining[col];
                queenRows[col] = -1;
                continue;
            }
            // Extract next bit
            ulong bit = remaining & (ulong)-(long)remaining;
            remaining &= remaining - 1; // clear lowest bit
            int row = FastTzcnt(bit);
            queenRows[col] = row;
            if (col >= pruneDepthGate && ShouldPrunePrefixFast(s.QueenRows, col, N, reflectionEnabled, prefixEnabled))
            {
                queenRows[col] = -1;
                continue;
            }
            if (incrementalEnabled && incScratch != null)
            {
                incScratch[0 * N + col] = row;
                incScratch[5 * N + col] = N - 1 - row;
            }
            if (col == 0) ReportRootProgress(ref s, request);
            if (!request.CountOnly) MaybeRaisePlacementEvent(ref s, request);
            // Push state
            s.StackCols[col] = cols;
            s.StackD1[col] = d1;
            s.StackD2[col] = d2;
            s.StackRemaining[col] = remaining;
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            col++;
            if (col == N) continue;
            ulong attacked = cols | d1 | d2;
            remaining = (~attacked) & s.Mask;
            if (symmetryActive)
            {
                ulong avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(N, col, s.QueenRows, remaining);
                if (aggressiveActive && col == 2 && queenRows[1] >= 0 && !((N & 1) == 1 && queenRows[0] == N / 2))
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
        // Persist local state back
        s.Col = col;
        s.Cols = cols;
        s.Diag1 = d1;
        s.Diag2 = d2;
        s.Remaining = remaining;
    }

    // Count-only specialized loop
    private static void MainLoopCountOnly(ref SearchState s, in Request request)
    {
        int N = s.N;
        int pruneDepthGate = int.MaxValue;
        bool prefixEnabled = SearchOptimizations.PrefixMinimalityPruningEnabled;
        bool reflectionEnabled = SearchOptimizations.ReflectionPrefixPruningEnabled;
        if (prefixEnabled || reflectionEnabled)
        {
            if (N >= 20) pruneDepthGate = 1;
            else if (N >= 16) pruneDepthGate = 2;
            else if (N >= 15) pruneDepthGate = 3;
        }
        // Locals for speed
        int col = s.Col;
        ulong cols = s.Cols;
        ulong d1 = s.Diag1;
        ulong d2 = s.Diag2;
        int[] rows = s.QueenRows;
        while (true)
        {
            if (request.IsCanceled()) break;
            if (col == N)
            {
                if (request.OnSolution != null && request.OnSolution(rows)) break;
                // backtrack
                col--;
                if (col < 0) break;
                cols = s.StackCols[col];
                d1 = s.StackD1[col];
                d2 = s.StackD2[col];
                s.Remaining = s.StackRemaining[col];
                rows[col] = -1;
                continue;
            }
            if (s.Remaining == 0)
            {
                col--;
                if (col < 0) break;
                cols = s.StackCols[col];
                d1 = s.StackD1[col];
                d2 = s.StackD2[col];
                s.Remaining = s.StackRemaining[col];
                rows[col] = -1;
                continue;
            }
            ulong rem = s.Remaining;
            ulong bit = rem & (ulong)-(long)rem; // lowest set bit
            rem &= rem - 1; // clear bit
            s.Remaining = rem;
            int row = FastTzcnt(bit);
            rows[col] = row;
            if (col >= pruneDepthGate && ShouldPrunePrefixFast(rows, col, N, reflectionEnabled, prefixEnabled))
            {
                rows[col] = -1;
                continue;
            }
            if (col == 0)
            {
                s.RootPlacements++;
                double pct = (double)s.RootPlacements / s.RootTotal * 100.0;
                request.ReportProgress(pct);
            }
            // push
            s.StackCols[col] = cols;
            s.StackD1[col] = d1;
            s.StackD2[col] = d2;
            s.StackRemaining[col] = rem;
            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;
            col++;
            if (col == N) continue;
            ulong attacked = cols | d1 | d2;
            s.Remaining = (~attacked) & s.Mask;
            if ((request.EnhancedSymmetry || request.AggressiveSymmetry) && N >= 14)
            {
                ulong avail = SymmetryHelper.ApplyAdvancedSymmetryPruning(N, col, rows, s.Remaining);
                if (request.AggressiveSymmetry && col == 2 && rows[1] >= 0 && !((N & 1) == 1 && rows[0] == N / 2))
                {
                    int minRow = rows[1];
                    if (minRow < N)
                    {
                        ulong lowerMask = (1UL << minRow) - 1UL;
                        avail &= ~lowerMask;
                    }
                    else avail = 0UL;
                }
                s.Remaining = avail;
            }
        }
        // Persist final state back
        s.Col = col;
        s.Cols = cols;
        s.Diag1 = d1;
        s.Diag2 = d2;
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
