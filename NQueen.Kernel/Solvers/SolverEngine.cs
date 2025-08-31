namespace NQueen.Kernel.Solvers;

public class SolverEngine(
    ISolutionFormatter solutionFormatter,
    Func<int, BoardState> boardStateFactory) : ISolver, IDisposable
{
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

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced =
        delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound =
        delegate { };

    public event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged =
        delegate { };

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public void CancelSimulation()
    {
        _cancellationTokenSource?.Cancel();
    }

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        return await Task.Run(() => GetResultsForCurrentConfigurationAsync(solutionMode, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    private void Initialize(int boardSize)
    {
        _board = _boardStateFactory(boardSize);
        _board.Reset();
        QueenPositions = _board.QueenPositions;
        Solutions = new HashSet<Memory<int>>(new MemoryIntArrayComparer());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<SimulationResults> GetResultsForCurrentConfigurationAsync(
        SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await SolveNQueenProblem(solutionMode, cancellationToken);
        stopwatch.Stop();
        var elapsedTimeInSec = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);

        return new SimulationResults(Solutions.Select(s => new Solution(s.ToArray(), _solutionFormatter)).ToList(), elapsedTimeInSec);
    }

    private async Task SolveNQueenProblem(SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        await SolveNQueenByModeAsync(0, solutionMode, cancellationToken);
    }

    private async Task SolveNQueenByModeAsync(
        int colIndex, SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        if (solutionMode == SolutionMode.Single)
        {
            ProgressValueChanged?.Invoke(this, new ProgressChangedWithTokenEventArgs(-1, _currentSimToken));
        }

        while (colIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                ProgressValueChanged?.Invoke(this, new ProgressChangedWithTokenEventArgs(0, _currentSimToken));
                return;
            }

            if (colIndex == BoardSize)
            {
                if (solutionMode == SolutionMode.Unique &&
                    SymmetryPruning.IsSymmetrical(QueenPositions, Solutions.ToList(), new MemoryIntArrayComparer()))
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

            var nextRow = await BoardState.FindValidQueenPositionAsync(
                colIndex, BoardSize, QueenPositions, cancellationToken, DelayInMillisec, DisplayMode);

            if (nextRow == -1)
            {
                colIndex--;
                continue;
            }

            QueenPositions.Span[colIndex] = nextRow;

            if (DisplayMode == DisplayMode.Visualize)
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions.ToArray()));

            colIndex++;
        }
    }

    private void AddSolutionAndNotify()
    {
        Solutions.Add(new Memory<int>(QueenPositions.ToArray()));
        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenPositions.ToArray()));
    }

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
            QueenPlaced = null!;
            SolutionFound = null!;
            ProgressValueChanged = null!;
            Solutions?.Clear();
            _cancellationTokenSource?.Dispose();
        }
    }

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
