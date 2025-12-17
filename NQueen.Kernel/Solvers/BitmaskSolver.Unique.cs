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
        // Fast visualization path: emit a first solution quickly and stop
        if (DisplayMode == DisplayMode.Visualize)
        {
            var rows = GenerateConstructiveSolution(BoardSize);
            if (!IsValidNQueenSolution(rows))
            {
                int[]? first = null;
                BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                    BoardSize,
                    RestrictFirstCol: false,
                    EnhancedSymmetry: false,
                    AggressiveSymmetry: false,
                    CountOnly: false,
                    DisplayMode,
                    DelayInMillisec: 0,
                    _currentSimToken,
                    () => IsSolverCanceled,
                    _ => { },
                    m => { if (EnableEvents) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
                    rowsFound =>
                    {
                        if (!ValidateRows(rowsFound)) return false;
                        first = rowsFound.ToArray();
                        return true;
                    }
                ));
                rows = first ?? rows;
            }
            if (ValidateRows(rows))
            {
                // Emit incremental QueenPlaced events
                if (EnableEvents)
                {
                    int n = rows.Length;
                    var prefix = new int[n];
                    Array.Fill(prefix, -1);
                    for (int depth = 1; depth <= n; depth++)
                    {
                        if (IsSolverCanceled) break;
                        prefix[depth - 1] = rows[depth - 1];
                        var snapshot = new int[n];
                        Array.Copy(prefix, snapshot, n);
                        QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(new Memory<int>(snapshot), BoardSize));
                    }
                }
                // Materialize and finalize
                _solutionCount = ExpectedSolutionCounts.GetUnique(BoardSize);
                MaterializeUniqueSingle(rows);
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
                return;
            }
        }

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
            rowsFound =>
            {
                if (!ValidateRows(rowsFound)) return false;
                UInt128 packed = 0;
                if (rowsFound.Length <= 25) packed = SymmetryHelper.GetCanonicalKey(rowsFound, _scratchBuffer!, out _);
                if (!seen.Add(packed)) return false;
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
                    {
                        _eventsSuppressedAfterCap = true;
                        return true;
                    }
                }
                return false;
            }
        ));
        _solutionCount = (ulong)seen.Count;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void MaterializeUniqueSingle(int[] rows)
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

    // Materialization path for Unique mode: delegate to unified executor
    private void EnumerateUniqueMaterializeAdaptive()
    {
        ExecuteUniqueModeUnified();
    }
}
