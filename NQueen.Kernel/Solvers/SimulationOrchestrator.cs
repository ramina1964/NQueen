namespace NQueen.Kernel.Solvers;

public class SimulationOrchestrator : ISolver, IDisposable
{
    public SimulationOrchestrator(SolverEngine solverEngine)
    {
        _solverEngine = solverEngine ??
            throw new ArgumentNullException(nameof(solverEngine));

        // Relay events from SolverEngine
        _solverEngine.QueenPlaced += (s, e) =>
            QueenPlaced?.Invoke(this, e);

        _solverEngine.SolutionFound += (s, e) =>
            SolutionFound?.Invoke(this, e);

        _solverEngine.ProgressValueChanged += (s, e) =>
            ProgressValueChanged?.Invoke(this, e);
    }

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

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced =
        delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound =
        delegate { };

    public event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged =
        delegate { };

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
        => await _solverEngine.GetResultsForBoardAsync(boardSize, solutionMode, displayMode);

    public void SetSimulationToken(Guid token) =>
        _solverEngine.SetSimulationToken(token);

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

    private bool _disposed = false;
    private readonly SolverEngine _solverEngine;
}