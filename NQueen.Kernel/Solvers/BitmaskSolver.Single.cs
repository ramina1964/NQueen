namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void SolveSingleMode() =>
        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
            rows =>
            {
                // Central invariant check (single place)
                if (!ValidateRows(rows)) return false;

                if (_solutions.Count == 0 && _largeBoardRawSolutions.Count == 0 && ShouldAddSolution())
                {
                    _solutionCount++;
                    if (rows.Length <= 25)
                    {
                        var packed = SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _);
                        _solutions.Add((packed, rows.Length));
                    }
                    else
                    {
                        // Keep raw copy for large boards (cannot pack into UInt128)
                        var copy = new int[rows.Length];
                        Array.Copy(rows, copy, rows.Length);
                        _largeBoardRawSolutions.Add(copy);
                    }
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
                    if (_capEnabled && (_solutions.Count + _largeBoardRawSolutions.Count) >= _maxDisplayedCount)
                        _eventsSuppressedAfterCap = true;
                    return true; // Stop after first solution
                }
                return false;
            }
        ));
}
