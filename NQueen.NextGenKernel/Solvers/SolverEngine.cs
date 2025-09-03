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

    public int NoOfSolutions => Solutions.Count;

    public int HalfBoardSize => _board.HalfBoardSize;

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound = delegate { };

    public event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged = delegate { };

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public async Task<SimulationResults> GetSimResultsAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        return await Task.Run(() => GetResultsForCurrentConfigurationAsync(solutionMode, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync(
        SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await SolveNQueenProblem(solutionMode, cancellationToken);
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

    private async Task<IEnumerable<Solution>> SolveNQueenProblem(
        SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        switch (solutionMode)
        {
            case SolutionMode.Single:
                await SolveNQueenByModeAsync(0, SolutionMode.Single, cancellationToken);
                break;

            case SolutionMode.Unique:
                await SolveNQueenByModeAsync(0, SolutionMode.Unique, cancellationToken);
                break;

            case SolutionMode.All:
                await FindAllSolutions(0, cancellationToken);
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

    private async Task SolveNQueenByModeAsync(
        int colIndex, SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        if (solutionMode == SolutionMode.Single)
        {
            // For 'Single' mode, set the progress bar to indeterminate
            ProgressValueChanged?.Invoke(this, new ProgressChangedWithTokenEventArgs(-1, _currentSimToken));
        }

        int solutionsFound = 0;

        while (colIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (QueenPositions[0] == HalfBoardSize)
                return;

            if (colIndex == BoardSize)
            {
                AddSolutionAndNotify();
                solutionsFound++;

                if (solutionMode == SolutionMode.Single)
                    return;

                NotifySolutionFound();

                // Update progress based on solutions found
                UpdateProgress(solutionsFound, BoardSize, solutionMode);

                colIndex--;
                continue;
            }

            QueenPositions[colIndex] = await BoardState.FindValidQueenPositionAsync(
                colIndex, BoardSize, QueenPositions, cancellationToken, DelayInMillisec, DisplayMode);

            if (QueenPositions[colIndex] == -1)
            {
                colIndex--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
            {
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions));
            }

            colIndex++;
        }

        // Ensure progress reaches 100% at the end of the simulation
        UpdateProgress(solutionsFound, BoardSize, solutionMode);
    }

    private void UpdateProgress(int solutionsFound, int boardSize, SolutionMode solutionMode)
    {
        // Get the total number of solutions for the given board size and solution mode
        int totalSolutions = ExpectedSolutionCount.GetCount(boardSize, solutionMode);

        if (totalSolutions == 0)
            return;

        // Calculate progress percentage
        int progress = Math.Min(solutionsFound * 100 / totalSolutions, 100); // Clamp progress to 100%

        // Throttle progress updates
        var now = DateTime.UtcNow;
        if (progress - _lastReportedProgress >= SimulationSettings.ProgressThresholdPct ||
            (now - _lastUpdateTime).TotalSeconds >= SimulationSettings.ProgressIntervalInSeconds)
        {
            ProgressValueChanged?.Invoke(this, new ProgressChangedWithTokenEventArgs(progress, _currentSimToken));
            _lastReportedProgress = progress;
            _lastUpdateTime = now;
        }
    }

    private async Task FindAllSolutions(int colIndex, CancellationToken cancellationToken)
    {
        await SolveNQueenByModeAsync(colIndex, SolutionMode.Unique, cancellationToken);

        var updates = new List<SolutionUpdateDTO>();

        foreach (var solution in Solutions)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var updateDTO = new SolutionUpdateDTO(BoardSize, SolutionMode.All, solution, Solutions);
            updates.Add(updateDTO);
        }

        foreach (var update in updates)
        {
            _solutionManager.UpdateSolutions(update);
        }
    }

    private void AddSolutionAndNotify()
    {
        var updateDTO = new SolutionUpdateDTO(
            BoardSize, SolutionMode, [.. QueenPositions], Solutions);

        _solutionManager.UpdateSolutions(updateDTO);
    }

    private void NotifySolutionFound()
    {
        if (DisplayMode == DisplayMode.Visualize)
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
