namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;
using NQueen.Kernel.Solvers.Counters;

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

        // Provide a predicate so parallel engine can skip materialization after cap reached.
        _parallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest(
            BoardSize,
            EnableEvents,
            1,
            ShouldAddSolution,
            rows =>
            {
                // Each callback corresponds to a unique canonical solution chosen to materialize.
                _solutionCount++; // count every unique whether materialized or not
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
                if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratchBuf, out var key, out _))
                {
                    _solutionCount++;
                    if (ShouldAddSolution())
                    {
                        // Materialize only when under cap: unpack canonical from key.
                        var canonicalRows = UnpackKeyToArray(key, N);
                        TryStoreSolution(canonicalRows, clone: false);
                    }
                }
                return false;
            }
        ));
    }

    private static int[] UnpackKeyToArray(UInt128 key, int n)
    {
        var rows = new int[n];
        for (int i = n - 1; i >= 0; i--)
        {
            rows[i] = (int)(key & 0x1F);
            key >>= 5;
        }
        return rows;
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
