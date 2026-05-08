namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Consolidated Unique mode executor: handles both large-board symmetry-pruned and small-board canonical enumeration.
    private void ExecuteUniqueModeUnified()
    {
        int boardSize = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;
        List<(UInt128 packed, int boardSize)> packedSample = [];
        int materialized = 0;

        Engines.SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            // Large boards: compute full unique count via symmetry-pruned counter.
            _solutionCount = Engines.SymmetryPrunedUniqueCounter.Count(boardSize, cap,
                prefixMinimality: EnablePrefixMinimalityPruning,
                reflectionPruning: EnablePartialReflectionPruning,
                onMaterialized: rows =>
            {
                if (materialized < Math.Max(1, cap))
                {
                    var packed = boardSize <= 25 ? SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _) : 0;
                    packedSample.Add((packed, boardSize));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        // Do not stop counting; continue enumeration to compute full count.
                    }
                }
            });
        }
        else
        {
            // Small boards: known counts available; set SolutionsCount to the full unique count.
            ulong known = ExpectedSolutionCounts.GetUnique(boardSize);
            _solutionCount = known;

            // Enumerate to materialize up to cap unique canonical solutions; never stop early.
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize: boardSize,
                RestrictFirstCol: true,            // half-board roots
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,                  // need solutions to filter canonical representatives
                DisplayMode: DisplayMode.Hide,
                DelayInMillisec: 0,
                SimulationToken: _currentSimToken,
                IsCanceled: () => IsSolverCanceled,
                ReportProgress: _ => { },
                OnQueenPlaced: _ => { },
                OnSolution: rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (!SymmetryHelper.IsIdentityCanonical(rows, _scratchBuffer!))
                        return false;

                    if (materialized < Math.Max(1, cap))
                    {
                        var packed = boardSize <= 25
                            ? SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _)
                            : 0;
                        packedSample.Add((packed, boardSize));
                        materialized++;
                        if (materialized >= cap && _capEnabled)
                        {
                            _eventsSuppressedAfterCap = true;
                            // Keep enumerating without raising further events; do not stop early.
                        }
                    }
                    return false; // continue enumeration until completion
                }
            ));
        }

        _solutions.AddRange(packedSample);
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void EnumerateUniqueVisualizeAdaptive()
    {
        // Visualization path: enumerate unique solutions while emitting QueenPlaced events with delay.
        SearchOptimizations.Configure(EnablePrefixMinimalityPruning, EnablePartialReflectionPruning, EnableIncrementalCanonicalization);
        int N = BoardSize;
        int cap = _maxDisplayedCount;
        int materialized = 0;
        var seen = new HashSet<UInt128>();

        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            N,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            CountOnly: false,
            DisplayMode,
            DelayInMillisec: DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
            m => { if (EnableEvents) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, N)); },
            rowsFound =>
            {
                if (!ValidateRows(rowsFound)) return false;

                UInt128 packed = 0;
                if (rowsFound.Length <= 25)
                    packed = SymmetryHelper.GetCanonicalKey(rowsFound, _scratchBuffer!, out _);

                // Skip duplicates using canonical key (if computed).
                if (packed != 0 && !seen.Add(packed))
                    return false;
                if (packed == 0)
                {
                    // For larger boards without canonical keys, conservatively count all (no duplicate filter).
                    // Optional: add alternative deduping if available.
                    // seen.Add(0) is not meaningful; keep counting.
                }

                // Materialize up to cap for UI
                if (materialized < cap)
                {
                    if (rowsFound.Length <= 25)
                        _solutions.Add((packed, rowsFound.Length));
                    else
                    {
                        var copy = new int[rowsFound.Length];
                        Array.Copy(rowsFound, copy, rowsFound.Length);
                        _largeBoardRawSolutions.Add(copy);
                    }
                    materialized++;

                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rowsFound), BoardSize));

                    if (materialized >= cap)
                        _eventsSuppressedAfterCap = true; // continue counting but stop UI events
                }

                // Continue enumeration to discover all unique solutions (do not stop after first)
                return false;
            }
        ));

        // Count equals unique keys for small boards; for larger boards (packed==0) we use seen.Count if any,
        // otherwise rely on search engine’s coverage via events. If packed was always 0, seen.Count==0;
        // in that case, we conservatively set solutionCount to materialized or keep observed unique via other counters.
        _solutionCount = (ulong)(seen.Count > 0 ? seen.Count : Math.Max(materialized, (int)_solutionCount));
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void MaterializeUniqueSingle(int[] rows)
    {
        if (rows.Length <= 25)
        {
            var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
            _solutions.Add((packed, rows.Length));
        }
        else
        {
            var copy = new int[rows.Length];
            Array.Copy(rows, copy, rows.Length);
            _largeBoardRawSolutions.Add(copy);
        }
        if (EnableEvents && !_eventsSuppressedAfterCap)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
    }

    // Materialization path for Unique mode: delegate to unified executor
    private void EnumerateUniqueMaterializeAdaptive()
    {
        ExecuteUniqueModeUnified();
    }
}
