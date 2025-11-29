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
        int capReachedFlag = 0;

        Engines.SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            _solutionCount = Engines.SymmetryPrunedUniqueCounter.Count(boardSize, cap, rows =>
            {
                if (materialized < Math.Max(1, cap))
                {
                    packedSample.Add((0, boardSize));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        System.Threading.Volatile.Write(ref capReachedFlag, 1);
                    }
                }
            });
        }
        else
        {
            ulong uniqueCount = Engines.CanonicalUniqueSearchEngine.CountUnique(boardSize, rows =>
            {
                if (System.Threading.Volatile.Read(ref capReachedFlag) == 1) return;
                if (materialized < Math.Max(1, cap))
                {
                    var packed = boardSize <= 25 ? SymmetryHelper.PackCanonical(rows, boardSize) : 0;
                    packedSample.Add((packed, boardSize));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        System.Threading.Volatile.Write(ref capReachedFlag, 1);
                    }
                }
            });
            _solutionCount = uniqueCount;
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
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => { if (EnableEvents) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
            m => { if (EnableEvents) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, N)); },
            rows =>
            {
                if (!ValidateRows(rows)) return false;
                // Canonical packing for uniqueness detection
                UInt128 packed = 0;
                if (rows.Length <= 25) packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                if (!seen.Add(packed)) return false; // skip duplicate canonical forms
                if (materialized < cap)
                {
                    if (rows.Length <= 25)
                        _solutions.Add((packed, rows.Length));
                    else
                    {
                        var copy = new int[rows.Length];
                        Array.Copy(rows, copy, rows.Length);
                        _largeBoardRawSolutions.Add(copy);
                    }
                    materialized++;
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                    if (materialized >= cap)
                    {
                        _eventsSuppressedAfterCap = true; // stops SolutionFound, but QueenPlaced continues
                        return true; // stop enumeration early (cap reached)
                    }
                }
                return false;
            }
        ));
        _solutionCount = (ulong)seen.Count; // number of unique canonical solutions enumerated (sampled up to cap)
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }
}
