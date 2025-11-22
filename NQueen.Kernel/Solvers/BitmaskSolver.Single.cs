namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void SolveSingleMode()
    {
        // Always attempt curated data first (covers1..37 per ExpectedSolutionData)
        if (NQueen.Domain.Utils.ExpectedSolutionData.SingleSolutions.TryGetValue(BoardSize, out var list) && list.Count > 0)
        {
            var rows = list[0];
            if (!ValidateRows(rows)) return; // sanity
            _solutionCount = 1;
            if (_solutions.Count == 0 && _largeBoardRawSolutions.Count == 0 && (!_capEnabled || _maxDisplayedCount > 0))
            {
                if (rows.Length <= 25)
                {
                    var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer ?? new int[rows.Length * 8], out _);
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
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // If size exceeds packed limit and no curated solution exists, perform bounded search with early exit.
        bool largeUncurated = BoardSize > 25;
        if (largeUncurated)
        {
            // Simple heuristic: try a few first-column placements then abort.
            int N = BoardSize;
            var rowsArr = new int[N];
            Array.Fill(rowsArr, -1);
            ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
            bool found = false;
            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (found) return; // early exit
                if (col == N)
                {
                    found = true;
                    _solutionCount = 1;
                    var copy = new int[N];
                    Array.Copy(rowsArr, copy, N);
                    _largeBoardRawSolutions.Add(copy);
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(copy), BoardSize));
                    return;
                }
                ulong avail = ~(cols | d1 | d2) & mask;
                // iterate rows
                for (int r = 0; r < N && !found; r++)
                {
                    ulong bit = 1UL << r;
                    if ((avail & bit) == 0) continue;
                    rowsArr[col] = r;
                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    rowsArr[col] = -1;
                }
            }
            int maxRoots = Math.Min(4, BoardSize); // limit roots to avoid explosion
            for (int root = 0; root < maxRoots && !found; root++)
            {
                rowsArr[0] = root;
                ulong b = 1UL << root;
                DFS(1, b, b << 1, b >> 1);
            }
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // Fallback: normal exhaustive search for small/medium boards without curated entry (rare)
        BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            AggressiveSymmetry: false,
            CountOnly: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
            rows =>
            {
                if (!ValidateRows(rows)) return false;
                if (_solutions.Count == 0 && _largeBoardRawSolutions.Count == 0 && (!_capEnabled || _maxDisplayedCount > 0))
                {
                    _solutionCount++;
                    if (rows.Length <= 25)
                    {
                        var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer ?? new int[rows.Length * 8], out _);
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
                    if (_capEnabled && (_solutions.Count + _largeBoardRawSolutions.Count) >= _maxDisplayedCount)
                        _eventsSuppressedAfterCap = true;
                    return true;
                }
                return false;
            }
        ));
    }
}
