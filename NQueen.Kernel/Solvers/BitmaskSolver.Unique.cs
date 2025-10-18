namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;
using NQueen.Kernel.Solvers.Counters;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// BitmaskSolver (Unique mode partial) - logic for unique enumeration & counting.
/// </summary>
public partial class BitmaskSolver
{
    // Thread-safe increment for parallel unique mode (ulong lacks direct Interlocked overload).
    private void IncrementSolutionCountAtomic()
    {
        // Unsafe cast to long for atomic increment; representation identical.
        Interlocked.Increment(ref Unsafe.As<ulong, long>(ref _solutionCount));
    }

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
            () => true, // Always allow counting for every unique solution
            rows =>
            {
                // Always increment for every callback (every unique solution)
                IncrementSolutionCountAtomic();
                // Only materialize if under cap and array is non-empty
                if (rows.Length > 0 && _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput)
                {
                    lock (_solutions)
                    {
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
        int estimatedUnique = EstimateUniqueSolutionCount(N);
        var uniqueKeys = new HashSet<UInt128>(estimatedUnique);
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
                    _solutionCount++; // Always increment for every unique solution found
                    // Only materialize if under cap
                    if (_solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput)
                    {
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
            int estimatedUnique = EstimateUniqueSolutionCount(N);
            var uniqueKeys = new HashSet<UInt128>(estimatedUnique);
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

    private static int EstimateUniqueSolutionCount(int boardSize)
    {
        // Empirical values for N=12..16, scale up for larger N
        return boardSize switch
        {
            12 => 14200,
            13 => 73712,
            14 => 365596,
            15 => 2279184,
            16 => 14772512,
            _ => 1000000 // fallback for larger N
        };
    }
}
