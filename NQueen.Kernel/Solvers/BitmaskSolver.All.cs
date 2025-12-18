namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllUnified(bool isParallel, int splitDepth)
    {
        int N = BoardSize;
        int cap = _capEnabled ? SimulationSettings.MaxDisplayedCount : int.MaxValue;

        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;

        // Visualization-aware path: emit incremental QueenPlaced events when visualizing
        if (DisplayMode == DisplayMode.Visualize && EnableEvents)
        {
            // Force sequential for smoother UI regardless of requested parallel flag
            bool restrictFirstCol = false;
            bool enhancedSymmetry = false;
            bool aggressiveSymmetry = false;
            int materialized = 0;
            ulong totalCount = 0;

            Engines.SearchOptimizations.Configure(
                prefixMinimality: EnablePrefixMinimalityPruning,
                reflectionPruning: EnablePartialReflectionPruning,
                incrementalCanonicalization: EnableIncrementalCanonicalization);

            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize: N,
                RestrictFirstCol: restrictFirstCol,
                EnhancedSymmetry: enhancedSymmetry,
                AggressiveSymmetry: aggressiveSymmetry,
                CountOnly: false, // visualization wants placement + solutions
                DisplayMode: DisplayMode,
                DelayInMillisec: DelayInMillisec,
                SimulationToken: _currentSimToken,
                IsCanceled: () => IsSolverCanceled,
                ReportProgress: p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
                OnQueenPlaced: m => { if (EnableEvents) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, N)); },
                OnSolution: rowsFound =>
                {
                    if (!ValidateRows(rowsFound)) return false;
                    totalCount++;

                    // Materialize up to cap for final display
                    if (materialized < cap)
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

                        materialized++;
                        if (!_eventsSuppressedAfterCap)
                            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rowsFound), N));

                        // Stop after cap to keep UI responsive
                        if (materialized >= cap && _capEnabled)
                        {
                            _eventsSuppressedAfterCap = true;
                            return true; // signal to stop
                        }
                    }
                    return false; // continue search
                }
            ));

            _solutionCount = totalCount;
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // Non-visualization path: CountOnly fast path or capped materialization using BitmaskSearchEngine
        Engines.SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

        ulong total = 0;
        int materializedCount = 0;

        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize: N,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            CountOnly: UseCountOnlyAllMode, // count-only path avoids materialization
            DisplayMode: DisplayMode.Hide,  // no visualization in non-visual path
            DelayInMillisec: 0,
            SimulationToken: _currentSimToken,
            IsCanceled: () => IsSolverCanceled,
            ReportProgress: p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            OnQueenPlaced: _ => { /* no-op in non-visual path */ },
            OnSolution: rowsFound =>
            {
                if (!ValidateRows(rowsFound)) return false;
                total++;

                if (!UseCountOnlyAllMode && materializedCount < Math.Max(1, cap))
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

                    if (materializedCount >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        return true; // stop after cap
                    }
                }

                return UseCountOnlyAllMode ? false : false;
            }
        ));

        _solutionCount = total;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }
}
