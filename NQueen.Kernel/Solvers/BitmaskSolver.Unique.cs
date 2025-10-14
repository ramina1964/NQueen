namespace NQueen.Kernel.Solvers;

/// <summary>
/// BitmaskSolver (Unique mode partial) - logic for unique enumeration & counting.
/// </summary>
public partial class BitmaskSolver
{
    private void RunUniqueParallel()
    {
        if (UseCountOnlyUniqueMode)
        {
            SolveUniqueCountOnlyMode();
            return;
        }

        _parallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest(
            BoardSize,
            EnableEvents,
            1,
            rows =>
            {
                _solutionCount++;
                if (ShouldAddSolution())
                {
                    lock (_solutions)
                    {
                        if (ShouldAddSolution())
                            TryStoreSolution(rows, clone: false);
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
        var uniqueKeys = new HashSet<UInt128>();
        var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];

        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: true,
            EnhancedSymmetry: true,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                var copy = (int[])rows.Clone();
                if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratchBuf, out _, out var canonical))
                {
                    _solutionCount++;
                    if (ShouldAddSolution())
                        TryStoreSolution(canonical.ToArray(), clone: false);
                }
                return false;
            }
        ));
    }

    private void SolveUniqueCountOnlyMode()
    {
        if (UseParallel)
        {
            _solutionCount = UniqueSolutionCounter.Count(BoardSize, null, _currentSimToken, ProgressValueChanged, this);
            _solutions.Clear();
        }
        else
        {
            int N = BoardSize;
            var uniqueKeys = new HashSet<UInt128>();
            var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];

            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: true,
                EnhancedSymmetry: true,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
                m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    var key = SymmetryHelper.GetCanonicalKey(rows, scratchBuf, out _);
                    uniqueKeys.Add(key);
                    return false;
                }
            ));
            _solutionCount = (ulong)uniqueKeys.Count;
            _solutions.Clear();
        }
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }
}
