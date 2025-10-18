namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private void RunAllParallel(int splitDepth)
    {
        int N = BoardSize;
        ulong totalCount = 0;
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        int limit = _capEnabled ? SimulationSettings.MaxNoOfSolutionsInOutput : int.MaxValue;
        _parallelEngine.RunAll(new BitmaskParallelEngine.AllRequest(
            BoardSize,
            splitDepth,
            EnableEvents,
            rows =>
            {
                // Central validation
                if (!ValidateRows(rows)) return;
                totalCount++;
                if (limit <= 0 || solutions.Count < limit)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                }
                // Always continue search (void callback)
            },
            pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
        ));
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
    }

    private void RunAllSequential()
    {
        var solutions = new List<(UInt128 packed, int boardSize)>();
        var rawSolutions = new List<int[]>();
        ulong totalCount = 0;
        int limit = _capEnabled ? SimulationSettings.MaxNoOfSolutionsInOutput : int.MaxValue;
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                if (!ValidateRows(rows)) return false; // skip malformed
                totalCount++;
                if (limit <= 0 || solutions.Count < limit)
                {
                    rawSolutions.Add(rows);
                    solutions.Add((0, rows.Length));
                }
                return false; // Always continue search
            }
        ));
        _solutionCount = totalCount;
        _solutions.Clear();
        _solutions.AddRange(solutions);
        _rawSolutions = rawSolutions;
    }

    private void SolveAllCountOnlyMode()
    {
        if (UseParallel)
        {
            ulong count = 0;
            _parallelEngine.RunAllCountOnly(new BitmaskParallelEngine.AllCountOnlyRequest(
                BoardSize,
                UseAdaptiveDepth ? -1 : ParallelRootSplitDepth,
                c => count = c,
                pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
            ));
            _solutionCount = count;
            _solutions.Clear();
        }
        else
        {
            ulong count = 0;
            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows => { if (ValidateRows(rows)) count++; return false; }
            ));
            _solutionCount = count;
            _solutions.Clear();
        }
    }
}
