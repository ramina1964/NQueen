namespace NQueen.Kernel.Solvers;

using NQueen.Kernel.Solvers.Engines;

/// <summary>
/// BitmaskSolver (Single mode partial) - contains logic for SolutionMode.Single.
/// </summary>
public partial class BitmaskSolver
{
    private void SolveSingleMode() =>
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
                if (_solutions.Count == 0 && ShouldAddSolution())
                {
                    _solutions.Add((int[])rows.Clone());
                    MaybeSuppressEventsAfterCap();
                }
                return true; // stop after first solution
            }
        ));
}
