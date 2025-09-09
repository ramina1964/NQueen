namespace NQueen.NextGenKernel.Solvers;

public class SimulationOrchestrator : IDisposable
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

    public int DelayInMillisec
    {
        get => _solverEngine.DelayInMillisec;
        set => _solverEngine.DelayInMillisec = value;
    }

    public int ProgressValue
    {
        get => _solverEngine.ProgressValue;
        set => _solverEngine.ProgressValue = value;
    }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs>
        SolutionFound = delegate { };

    public event EventHandler<ProgressUpdateEventArgs>
        ProgressValueChanged = delegate { };

    public async Task<SimulationResults> GetSimResultsAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        return await _solverEngine.GetSimResultsAsync(boardSize, solutionMode, displayMode);
    }

    #endregion Implementation

    public void SetSimulationToken(Guid token) =>
        _solverEngine.SetSimulationToken(token);

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

    #endregion IDisposable Implementation

    public void RaiseProgressValueChangedForTest(int progress, Guid token) =>
        ProgressValueChanged?.Invoke(this,
            new ProgressUpdateEventArgs(progress, token));

    private bool _disposed = false;
    private readonly SolverEngine _solverEngine;
}