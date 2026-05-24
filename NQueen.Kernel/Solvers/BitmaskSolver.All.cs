namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllUnified()
    {
        int N = BoardSize;

        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;

        // Non-visualization path: CountOnly fast path or capped materialization
        ulong total = 0;
        int materializedCount = 0;

        // Enforce a hard cap in non-visual materialization, independent of _capEnabled
        int effectiveCap = UseCountOnlyAllMode ? 0 : SimulationSettings.MaxDisplayedCount;

        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize: N,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            CountOnly: UseCountOnlyAllMode,
            DisplayMode: DisplayMode.Hide,
            DelayInMillisec: 0,
            SimulationToken: _currentSimToken,
            IsCanceled: () => IsSolverCanceled,
            ReportProgress: p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            OnQueenPlaced: _ => { /* no-op in non-visual path */ },
            OnSolution: rowsFound =>
            {
                if (!ValidateRows(rowsFound)) return false;
                total++;

                if (!UseCountOnlyAllMode && materializedCount < Math.Max(1, effectiveCap))
                {
                    if (rowsFound.Length <= 25)
                    {
                        var packed = SymmetryHelper.GetCanonicalKey(rowsFound, _scratchBuffer!, out _);
                        _solutions.Add((packed, rowsFound.Length));
                    }
                    else
                    {
                        var copy = new int[rowsFound.Length];
                        Array.Copy(rowsFound, copy, rowsFound.Length);
                        _largeBoardRawSolutions.Add(copy);
                    }

                    materializedCount++;

                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rowsFound), N));

                    if (materializedCount >= effectiveCap)
                    {
                        // Do NOT stop the engine; keep counting, but suppress further materialization/events
                        _eventsSuppressedAfterCap = true;
                        return false;
                    }
                }

                return false;
            },
            PrefixMinimalityPruning: EnablePrefixMinimalityPruning,
            ReflectionPruning: EnablePartialReflectionPruning
        ));

        _solutionCount = total;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    // Wrapper used by HandleModeCommon to select parallel mode and split depth.
    private void EnumerateAllAdaptive(bool countOnly)
    {
        if (countOnly)
        {
            // Optimized All count-only: symmetry-reduced bitboard with parallel top-level split
            _solutionCount = (ulong)BitboardNQueenSolver.CountSolutions(BoardSize, parallel: true);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // For large N, RunAllUnified visits every one of the ~39B solutions sequentially with
        // no half-board symmetry reduction (e.g. 1343s for N=20). The two-phase approach:
        //   1. Early-exit DFS to collect the display sample (near-instant).
        //   2. BitboardNQueenSolver.CountSolutions with half-board symmetry (~19.5B nodes
        //      instead of 39B) — roughly 2x faster even when sequential, and much faster
        //      in parallel.
        // Remove the UseParallel guard so both parallel and sequential configurations
        // benefit; UseParallel is passed through to CountSolutions inside.
        if (BoardSize >= SimulationSettings.ParallelAllMaterializeAutoEnableThresholdN)
        {
            CollectAllSamplesAndCountParallel();
            return;
        }

        RunAllUnified();
    }

    // Phase 1: collect up to cap solutions via an early-exit DFS (completes in milliseconds).
    // Phase 2: count all solutions with the parallel half-board bitboard counter.
    private void CollectAllSamplesAndCountParallel()
    {
        int N = BoardSize;
        // Mirror RunAllUnified's effectiveCap: always collect at least one sample regardless of
        // _capEnabled (uncapped test solvers still need solutions in the result).
        int cap = Math.Max(1, SimulationSettings.MaxDisplayedCount);
        CollectAllSampleSolutionsDFS(N, cap);

        _solutionCount = (ulong)BitboardNQueenSolver.CountSolutions(N, parallel: UseParallel);
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    // Runs a minimal DFS that stops as soon as cap solutions are stored.
    private void CollectAllSampleSolutionsDFS(int N, int cap)
    {
        ulong mask = N == 64 ? ulong.MaxValue : (1UL << N) - 1UL;
        int[] rows = new int[N];
        Array.Fill(rows, -1);
        int materialized = 0;

        DFS(0, 0UL, 0UL, 0UL);

        void DFS(int col, ulong cols, ulong d1, ulong d2)
        {
            if (materialized >= cap || IsSolverCanceled) return;
            if (col == N)
            {
                if (N <= 25)
                {
                    var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                    _solutions.Add((packed, N));
                }
                else
                {
                    var copy = new int[N];
                    Array.Copy(rows, copy, N);
                    _largeBoardRawSolutions.Add(copy);
                }
                if (EnableEvents && !_eventsSuppressedAfterCap)
                    SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), N));
                materialized++;
                if (materialized >= cap)
                    _eventsSuppressedAfterCap = true;
                return;
            }
            ulong avail = ~(cols | d1 | d2) & mask;
            while (avail != 0 && materialized < cap && !IsSolverCanceled)
            {
                ulong bit = avail & (ulong)-(long)avail;
                avail ^= bit;
                rows[col] = BitOperations.TrailingZeroCount(bit);
                DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
            }
            rows[col] = -1;
        }
    }
}
