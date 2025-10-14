namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;
using NQueen.Kernel.Solvers.Heuristics;
using NQueen.Kernel.Solvers.Counters;

/// <summary>
/// BitmaskSolver (All mode partial) - logic for enumerating or counting all solutions (with symmetry).
/// </summary>
public partial class BitmaskSolver
{
    private void RunAllParallel(int splitDepth)
    {
        _parallelEngine.RunAll(new BitmaskParallelEngine.AllRequest(
            BoardSize,
            EnableEvents,
            splitDepth < 1 ? 1 : splitDepth,
            rows =>
            {
                Interlocked.Increment(ref _solutionCount);
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

    private void RunAllSequential()
    {
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                _solutionCount++;
                if (ShouldAddSolution())
                    TryStoreSolution(rows, clone: true); // need copy
                return false; // continue search
            }
        ));
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
                m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows => { count++; return false; }
            ));
            _solutionCount = count;
            _solutions.Clear();
        }
    }
}
