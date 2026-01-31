namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllUnified(bool isParallel, int splitDepth)
    {
        int N = BoardSize;

        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;

        // Non-visualization path: CountOnly fast path or capped materialization
        SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

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
                        var packed = SymmetryHelper.GetCanonicalKey(rowsFound, _scratchBuffer ?? new int[rowsFound.Length * 8], out _);
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
            }
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

        int splitDepth = UseAdaptiveDepth
            ? ParallelSplitDepthHeuristic.GetOptimalSplitDepth(BoardSize)
            : Math.Max(1, ParallelRootSplitDepth);

        bool parallel = UseParallel && ParallelSplitDepthHeuristic.ShouldUseParallelForAll(BoardSize);
        RunAllUnified(isParallel: parallel, splitDepth: splitDepth);
    }
}

