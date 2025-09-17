namespace NQueen.Kernel.Solvers;

public class SolverOrchestrator : ISolverPruning, IDisposable
{
    // Constructor
    public SolverOrchestrator(ISolverPruning solver)
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

    private void RelaySolverEngineEvents()
    {
        _queenPlacedHandler = (s, e) => QueenPlaced?.Invoke(this, e);
        _solutionFoundHandler = (s, e) => SolutionFound?.Invoke(this, e);
        _progressValueChangedHandler = (s, e) => ProgressValueChanged?.Invoke(this, e);

        _solver.QueenPlaced += _queenPlacedHandler;
        _solver.SolutionFound += _solutionFoundHandler;
        _solver.ProgressValueChanged += _progressValueChangedHandler;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            if (_queenPlacedHandler != null)
                _solver.QueenPlaced -= _queenPlacedHandler;

            if (_solutionFoundHandler != null)
                _solver.SolutionFound -= _solutionFoundHandler;

            if (_progressValueChangedHandler != null)
                _solver.ProgressValueChanged -= _progressValueChangedHandler;

            _queenPlacedHandler = null;
            _solutionFoundHandler = null;
            _progressValueChangedHandler = null;

            _disposed = true;
            if (_solver is IDisposable disposableSolver)
                disposableSolver.Dispose();
        }
    }

    // Private Methods
    private EventHandler<Domain.EventArgsPruning.QueenPlacedEventArgs>? _queenPlacedHandler;
    private EventHandler<Domain.EventArgsPruning.SolutionFoundEventArgs>? _solutionFoundHandler;
    private EventHandler<Domain.EventArgsPruning.ProgressUpdateEventArgs>? _progressValueChangedHandler;

    private bool _disposed = false;
    private readonly ISolverPruning _solver;
}