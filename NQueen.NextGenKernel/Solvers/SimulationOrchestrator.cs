namespace NQueen.NextGenKernel.Solvers;

public class SimulationOrchestrator : ISolver, IDisposable
{
    public SimulationOrchestrator(
        SolverEngine solverEngine)
    {
        _solverEngine = solverEngine ??
            throw new ArgumentNullException(nameof(solverEngine));

        // Relay events from SolverEngine
        _solverEngine.QueenPlaced += (s, e) => QueenPlaced?.Invoke(this, e);
        _solverEngine.SolutionFound += (s, e) => SolutionFound?.Invoke(this, e);
        _solverEngine.ProgressValueChanged += (s, e) => ProgressValueChanged?.Invoke(this, e);
    }

    #region ISolver Implementation

    public bool IsSolverCanceled
    {
        get => _solverEngine.IsSolverCanceled;
        set => _solverEngine.IsSolverCanceled = value;
    }

    public int DelayInMilliseconds
    {
        get => _solverEngine.DelayInMilliseconds;
        set => _solverEngine.DelayInMilliseconds = value;
    }

    public double ProgressValue
    {
        get => _solverEngine.ProgressValue;
        set => _solverEngine.ProgressValue = value;
    }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs>
        SolutionFound = delegate { };

    public event EventHandler<ProgressValueChangedWithTokenEventArgs>
        ProgressValueChanged = delegate { };

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
        => await _solverEngine.GetResultsForBoardAsync(boardSize, solutionMode, displayMode);

    #endregion

    #region Orchestrator API

    public void SetSimulationToken(Guid token) =>
        _solverEngine.SetSimulationToken(token);

    public int BoardSize => _solverEngine.BoardSize;

    public int[] QueenPositions => _solverEngine.QueenPositions;

    public int NoOfSolutions => _solverEngine.NoOfSolutions;

    public int HalfBoardSize => _solverEngine.HalfBoardSize;

    public int SolutionsPerUpdate => _solverEngine.SolutionsPerUpdate;

    public SolutionMode SolutionMode
    {
        get => _solverEngine.SolutionMode;
        set => _solverEngine.SolutionMode = value;
    }
    public DisplayMode DisplayMode
    {
        get => _solverEngine.DisplayMode;
        set => _solverEngine.DisplayMode = value;
    }
    public HashSet<int[]> Solutions => _solverEngine.Solutions;

    public int GetHalfSize() => _solverEngine.GetHalfSize();

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync()
        => await _solverEngine.GetResultsForCurrentConfigurationAsync();

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
        if (disposing)
        {
            _solverEngine.Dispose();
        }
    }

    #endregion

    public void RaiseProgressValueChangedForTest(double progress, Guid token) =>
        ProgressValueChanged?.Invoke(this,
            new ProgressValueChangedWithTokenEventArgs(progress, token));

    private bool _disposed = false;
    private readonly SolverEngine _solverEngine;
}