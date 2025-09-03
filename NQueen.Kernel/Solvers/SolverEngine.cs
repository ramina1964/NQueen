namespace NQueen.Kernel.Solvers;

public class SolverEngine(
    ISolutionFormatter solutionFormatter,
    Func<int, BoardState> boardStateFactory) : ISolver, IDisposable
{
    // Properties
    public bool IsSolverCanceled
    {
        get => _cancellationTokenSource?.IsCancellationRequested ?? false;
        set
        {
            if (value)
                _cancellationTokenSource?.Cancel();
            else
                _cancellationTokenSource = new CancellationTokenSource();
        }
    }

    public int DelayInMillisec { get; set; }
    public int ProgressValue { get; set; }
    public int BoardSize => _board.BoardSize;
    public Memory<int> QueenPositions { get; private set; } = Memory<int>.Empty;
    public int NoOfSolutions => Solutions.Count;
    public SolutionMode SolutionMode { get; set; }
    public DisplayMode DisplayMode { get; set; }

    // Events
    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };
    public event EventHandler<SolutionFoundEventArgs> SolutionFound = delegate { };
    public event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged = delegate { };

    // Public Methods
    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public void CancelSimulation() => _cancellationTokenSource?.Cancel();

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        return await Task.Run(() => RunSimulationAsync(solutionMode, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Private Methods
    private void Initialize(int boardSize)
    {
        _board = _boardStateFactory(boardSize);
        _board.Reset();
        QueenPositions = _board.QueenPositions;
        Solutions = new HashSet<Memory<int>>(new MemoryIntArrayComparer());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<SimulationResults> RunSimulationAsync(SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        var elapsedTime = await MeasureExecutionTimeAsync(async () =>
        {
            await SolveNQueenProblemAsync(solutionMode, cancellationToken);
        });

        return new SimulationResults(
            Solutions.Select(s => new Solution(s.ToArray(), _solutionFormatter)).ToList(),
            elapsedTime);
    }

    private async Task SolveNQueenProblemAsync(SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        await SolveByModeAsync(0, solutionMode, cancellationToken);
    }

    private async Task SolveByModeAsync(int colIndex, SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        if (solutionMode == SolutionMode.Single)
        {
            NotifyProgress(-1);
        }

        while (colIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                NotifyProgress(0);
                return;
            }

            if (colIndex == BoardSize)
            {
                if (solutionMode == SolutionMode.Unique && IsSymmetricalSolution())
                {
                    colIndex--;
                    continue;
                }

                AddSolutionAndNotify();
                if (solutionMode == SolutionMode.Single)
                    return;

                colIndex--;
                continue;
            }

            var nextRow = await FindNextRowAsync(colIndex, cancellationToken);

            if (nextRow == -1)
            {
                colIndex--;
                continue;
            }

            QueenPositions.Span[colIndex] = nextRow;

            if (DisplayMode == DisplayMode.Visualize)
                NotifyQueenPlaced();

            colIndex++;
        }
    }

    private async Task<int> FindNextRowAsync(int colIndex, CancellationToken cancellationToken)
    {
        return await BoardState.FindValidQueenPositionAsync(
            colIndex, BoardSize, QueenPositions, cancellationToken, DelayInMillisec, DisplayMode);
    }

    private bool IsSymmetricalSolution()
    {
        return SymmetryPruning.IsSymmetrical(QueenPositions, Solutions.ToList(), new MemoryIntArrayComparer());
    }

    private void AddSolutionAndNotify()
    {
        Solutions.Add(new Memory<int>(QueenPositions.ToArray()));
        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenPositions.ToArray()));
    }

    private void NotifyProgress(int progress)
    {
        ProgressValueChanged?.Invoke(this, new ProgressChangedWithTokenEventArgs(progress, _currentSimToken));
    }

    private void NotifyQueenPlaced()
    {
        QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions.ToArray()));
    }

    private async Task<double> MeasureExecutionTimeAsync(Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
        if (disposing)
        {
            QueenPlaced = null!;
            SolutionFound = null!;
            ProgressValueChanged = null!;
            Solutions?.Clear();
            _cancellationTokenSource?.Dispose();
        }
    }

    // Fields
    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter
        ?? throw new ArgumentNullException(nameof(solutionFormatter));

    private readonly Func<int, BoardState> _boardStateFactory = boardStateFactory
        ?? throw new ArgumentNullException(nameof(boardStateFactory));

    private BoardState _board = null!;
    private CancellationTokenSource _cancellationTokenSource = new();
    private HashSet<Memory<int>> Solutions { get; set; } = new(new MemoryIntArrayComparer());
    private Guid _currentSimToken = Guid.Empty;
    private bool _disposed = false;
}
