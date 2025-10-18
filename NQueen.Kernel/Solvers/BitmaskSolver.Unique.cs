namespace NQueen.Kernel.Solvers;


public partial class BitmaskSolver
{
    private void RunUniqueParallel()
    {
        if (UseCountOnlyUniqueMode)
        {
            SolveUniqueCountOnlyMode();
            return;
        }

        int limit = _capEnabled ? SimulationSettings.MaxNoOfSolutionsInOutput : int.MaxValue;
        _parallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest(
            BoardSize,
            EnableEvents,
            1,
            () => true,
            rows =>
            {
                IncrementSolutionCountAtomic();
                if (rows.Length > 0 && _solutions.Count < limit)
                {
                    var packed = rows.Length <= 25 ? SymmetryHelper.GetCanonicalKey(rows, new int[rows.Length * 2], out _) : 0;
                    lock (_solutions)
                    {
                        _solutions.Add((packed, rows.Length));
                    }
                }
            },
            pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
        ));
    }

    private void RunUniqueSequential()
    {
        if (UseCountOnlyUniqueMode)
        {
            SolveUniqueCountOnlyMode();
            return;
        }

        int N = BoardSize;
        int estimatedUnique = BitmaskSolver.EstimateUniqueSolutionCount(N);
        var uniqueKeys = new HashSet<UInt128>(estimatedUnique);
        var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];
        var solutions = new List<(UInt128 packed, int boardSize)>();
        int limit = _capEnabled ? SimulationSettings.MaxNoOfSolutionsInOutput : int.MaxValue;

        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: true,
            EnhancedSymmetry: true,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                var copy = (int[])rows.Clone();
                if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratchBuf, out var key, out _))
                {
                    _solutionCount++;
                    if (solutions.Count < limit)
                    {
                        var packed = copy.Length <= 25 ? key : 0;
                        solutions.Add((packed, copy.Length));
                    }
                }
                return false;
            }
        ));
        _solutions.Clear();
        _solutions.AddRange(solutions);
    }
}
