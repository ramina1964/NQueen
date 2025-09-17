namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine(
    ISolutionManager solutionManager,
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

    public int[] QueenPositions { get; private set; } = [];

    public int HalfBoardSize => _board.HalfBoardSize;

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound = delegate { };

    public event EventHandler<ProgressUpdateEventArgs> ProgressValueChanged = delegate { };

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public async Task<SimulationResults> GetSimResultsAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        return await Task.Run(() => GetResultsForCurrentConfigurationAsync(solutionMode,
            displayMode, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync(
        SolutionMode solutionMode, DisplayMode displayMode, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await SolveNQueenProblem(solutionMode, displayMode, cancellationToken);
        stopwatch.Stop();
        var elapsedTimeInSec = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);

        return new SimulationResults(solutions, elapsedTimeInSec);
    }

    private void Initialize(int boardSize)
    {
        _board = _boardStateFactory(boardSize);
        _board.Reset();
        QueenPositions = _board.QueenPositions;
        Solutions = new HashSet<int[]>(new IntArrayComparer());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<IEnumerable<Solution>> SolveNQueenProblem(SolutionMode solutionMode,
        DisplayMode displayMode, CancellationToken cancellationToken)
    {
        switch (solutionMode)
        {
            case SolutionMode.Single:
                await SolveNQueenByModeAsync(0, SolutionMode.Single,
                    displayMode, cancellationToken);
                break;

            case SolutionMode.Unique:
                await SolveNQueenByModeAsync(0, SolutionMode.Unique, displayMode,
                    cancellationToken);
                break;

            case SolutionMode.All:
                await FindAllSolutions(0, displayMode, cancellationToken);
                break;

            default:
                throw new NotImplementedException();
        }

        var result = new List<Solution>();
        var index = 1;

        foreach (var solution in Solutions)
        {
            if (SolutionMode == SolutionMode.Single && result.Count == 1)
                break;

            result.Add(new Solution(solution, _solutionFormatter, index++));
        }

        return result;
    }

    private async Task SolveNQueenByModeAsync(int columnIndex, SolutionMode solutionMode,
        DisplayMode displayMode, CancellationToken cancellationToken)
    {
        int solutionsFound = 0;

        while (columnIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Terminate simulation if the first column's queen position exceeds HalfBoardSize
            if (columnIndex == 0 && QueenPositions[columnIndex] >= HalfBoardSize)
                return;

            if (columnIndex == BoardSize)
            {
                AddSolutionAndNotify();
                solutionsFound++;

                if (solutionMode == SolutionMode.Single)
                    return;

                NotifySolutionFound(displayMode);
                UpdateProgress(solutionsFound, BoardSize, solutionMode);

                columnIndex--;
                continue;
            }

            QueenPositions[columnIndex] = await BoardState.FindValidQueenPositionAsync(
                columnIndex, BoardSize, QueenPositions, cancellationToken, DelayInMillisec,
                displayMode);

            if (QueenPositions[columnIndex] == -1)
            {
                columnIndex--;
                continue;
            }

            if (displayMode == DisplayMode.Visualize)
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions));

            columnIndex++;
        }

        UpdateProgress(solutionsFound, BoardSize, solutionMode);
    }

    private void UpdateProgress(int solutionsFound, int boardSize, SolutionMode solutionMode)
    {
        int totalSolutions = ExpectedSolutionCount.GetCount(boardSize, solutionMode);

        if (totalSolutions == 0)
            return;

        int progress = Math.Min(solutionsFound * 100 / totalSolutions, 100);

        var now = DateTime.UtcNow;
        if (progress - _lastReportedProgress >= SimulationSettings.ProgressThresholdPct ||
            (now - _lastUpdateTime).TotalMilliseconds >=
            SimulationSettings.ProgressIntervalInMilliSec)
        {
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(progress, _currentSimToken));
            _lastReportedProgress = progress;
            _lastUpdateTime = now;
        }
    }

    private async Task FindAllSolutions(int columnIndex, DisplayMode displayMode,
        CancellationToken cancellationToken)
    {
        await SolveNQueenByModeAsync(columnIndex, SolutionMode.Unique,
            displayMode, cancellationToken);

        var updates = new List<SolutionUpdateDTO>();

        foreach (var solution in Solutions)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var updateDTO = new SolutionUpdateDTO(BoardSize, SolutionMode.All,
                solution, Solutions);

            updates.Add(updateDTO);
        }

        foreach (var update in updates)
            _solutionManager.UpdateSolutions(update);
    }

    private void AddSolutionAndNotify()
    {
        var updateDTO = new SolutionUpdateDTO(
            BoardSize, SolutionMode, [.. QueenPositions], Solutions);

        _solutionManager.UpdateSolutions(updateDTO);
    }

    private void NotifySolutionFound(DisplayMode displayMode)
    {
        if (displayMode == DisplayMode.Visualize)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenPositions));
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

    private readonly ISolutionManager _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));
    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter ?? throw new ArgumentNullException(nameof(solutionFormatter));
    private readonly Func<int, BoardState> _boardStateFactory = boardStateFactory ?? throw new ArgumentNullException(nameof(boardStateFactory));

    private BoardState _board = null!;
    private CancellationTokenSource _cancellationTokenSource = new();

    private HashSet<int[]> Solutions { get; set; } = new(new IntArrayComparer());

    private Guid _currentSimToken = Guid.Empty;

    private bool _disposed = false;

    private int _lastReportedProgress = 0;
    private DateTime _lastUpdateTime = DateTime.MinValue;
}
