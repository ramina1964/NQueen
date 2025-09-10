namespace NQueen.Kernel.Solvers;

public class SimulationOrchestrator : ISolverFrontEndPruning, IDisposable
{
    // Constructor
    public SimulationOrchestrator(SolverEngine solver)
    {
        _solver = solver
            ?? throw new ArgumentNullException(nameof(solver));

        // Relay events from SolverEngine
        RelaySolverEngineEvents();
    }

    // Properties
    public bool IsSolverCanceled
    {
        get => _solver.IsSolverCanceled;
        set => _solver.IsSolverCanceled = value;
    }

    public int DelayInMillisec
    {
        get => _solver.DelayInMillisec;
        set => _solver.DelayInMillisec = value;
    }

    public int ProgressValue
    {
        get => _solver.ProgressValue;
        set => _solver.ProgressValue = value;
    }

    // Events
    public event EventHandler<Domain.EventArgsPruning.QueenPlacedEventArgs> QueenPlaced
        = delegate { };

    public event EventHandler<Domain.EventArgsPruning.SolutionFoundEventArgs> SolutionFound
        = delegate { };

    public event EventHandler<Domain.EventArgsPruning.ProgressUpdateEventArgs> ProgressValueChanged
        = delegate { };

    // Public Methods
    public async Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
        => await _solver.GetSimResultsAsync(simContext);

    public void SetSimulationToken(Guid token) =>
        _solver.SetSimulationToken(token);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Private Methods
    private void RelaySolverEngineEvents()
    {
        _solver.QueenPlaced += (s, e) =>
            QueenPlaced?.Invoke(this, e);

        _solver.SolutionFound += (s, e) =>
            SolutionFound?.Invoke(this, e);

        _solver.ProgressValueChanged += (s, e) =>
            ProgressValueChanged?.Invoke(this, e);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
        if (disposing)
        {
            _solver.Dispose();
        }
    }

    // Private Fields
    private bool _disposed = false;
    private readonly SolverEngine _solver;
}