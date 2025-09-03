namespace NQueen.Kernel.Solvers;

public class SimulationOrchestrator : ISolver, IDisposable
{
    // Constructor
    public SimulationOrchestrator(SolverEngine solverEngine)
    {
        _solverEngine = solverEngine ?? throw new ArgumentNullException(nameof(solverEngine));

        // Relay events from SolverEngine
        RelaySolverEngineEvents();
    }

    // Properties
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

    // Events
    public event EventHandler<QueenPlacedEventArgs> QueenPlaced =
        delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound =
        delegate { };

    public event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged =
        delegate { };

    // Public Methods
    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide) =>
        await _solverEngine.GetResultsForBoardAsync(boardSize, solutionMode, displayMode);

    public void SetSimulationToken(Guid token) =>
        _solverEngine.SetSimulationToken(token);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Private Methods
    private void RelaySolverEngineEvents()
    {
        _solverEngine.QueenPlaced += (s, e) =>
            QueenPlaced?.Invoke(this, e);
        
        _solverEngine.SolutionFound += (s, e) =>
            SolutionFound?.Invoke(this, e);
        
        _solverEngine.ProgressValueChanged += (s, e) =>
            ProgressValueChanged?.Invoke(this, e);
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

    // Private Fields
    private bool _disposed = false;
    private readonly SolverEngine _solverEngine;
}