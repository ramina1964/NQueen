namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine(
    ISolutionManager solutionManager,
    Func<int, BoardState> boardStateFactory) : ISolver, IDisposable
{
    #region ISolver Implementation

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

    public double ProgressValue { get; set; }

    public int BoardSize => _board.BoardSize;

    public int[] QueenPositions { get; private set; } = [];

    // Todo: Either use this property or remove it.
    public int NoOfSolutions => Solutions.Count;

    public int HalfBoardSize => _board.HalfBoardSize;

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public HashSet<int[]> Solutions { get; private set; } = _hashSet;

    public event EventHandler<QueenPlacedEventArgs>
        QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound
        = delegate { };

    public event EventHandler<ProgressChangedWithTokenEventArgs>
        ProgressValueChanged = delegate { };

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        // Pass the cancellation token to the async workflow
        return await Task.Run(() => GetResultsForCurrentConfigurationAsync(
            _cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync()
        => await GetResultsForCurrentConfigurationAsync(_cancellationTokenSource.Token);

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await SolveNQueenProblem(cancellationToken);
        stopwatch.Stop();
        var elapsedTimeInSec = Math.Round((double)stopwatch.Elapsed.TotalSeconds, 1);

        return new SimulationResults(solutions, elapsedTimeInSec);
    }

    #endregion

    public int GetHalfSize() => _board.HalfBoardSize;

    #region Private Methods

    private void Initialize(int boardSize)
    {
        _board = _boardStateFactory(boardSize);
        _board.Reset();
        QueenPositions = _board.QueenPositions;
        Solutions = new HashSet<int[]>(new IntArrayComparer());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<IEnumerable<Solution>> SolveNQueenProblem(CancellationToken cancellationToken)
    {
        switch (SolutionMode)
        {
            case SolutionMode.Single:
                await SolveNQueenForModeAsync(0, SolutionMode.Single, cancellationToken);
                break;
            case SolutionMode.Unique:
                await SolveNQueenForModeAsync(0, SolutionMode.Unique, cancellationToken);
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
            result.Add(new Solution(solution, index++));
        }
        return result;
    }

    private async Task SolveNQueenForModeAsync(int colIndex, SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        var totalNoOfSolutions = ExpectedSolutionCount.GetCount(BoardSize, solutionMode);
        while (colIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (QueenPositions[0] == HalfBoardSize)
                return;

            if (colIndex == BoardSize && solutionMode == SolutionMode.Single)
            {
                AddSolutionAndNotify();
                NotifySolutionFound();
                return;
            }
            if (colIndex == BoardSize && (solutionMode == SolutionMode.Unique || solutionMode == SolutionMode.All))
            {
                AddSolutionAndNotify();
                NotifySolutionFound();

                colIndex--;
                continue;
            }

            QueenPositions[colIndex] = await BoardState.FindValidQueenPositionAsync(
                colIndex, BoardSize, QueenPositions, cancellationToken,
                DelayInMillisec, DisplayMode);

            if (QueenPositions[colIndex] == -1)
            {
                colIndex--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
            {
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions));
                ReportProgress(totalNoOfSolutions, solutionMode); // Pass solutionMode here
            }

            colIndex++;
        }

        // Ensure final progress update after all solutions are processed
        ReportProgress(totalNoOfSolutions, solutionMode);
    }

    private void ReportProgress(int totalNoOfSolutions, SolutionMode mode)
    {
        if (totalNoOfSolutions > 0)
        {
            // Adjust progress calculation based on mode
            var scalingFactor = mode == SolutionMode.All ? 1.0 : 8.0;
            var adjustedTotalSolutions = totalNoOfSolutions / scalingFactor;

            // Use linear scaling for progress calculation
            var scaledTotalSolutions = Math.Max(adjustedTotalSolutions, 1); // Avoid division by zero

            // Calculate progress as a percentage and round to 0 decimal places
            var currentProgress = Math.Clamp(
                Math.Round((Solutions.Count / scaledTotalSolutions) * 100, 0), 0, 100);

            // Ensure progress doesn't reach 100% until the simulation is complete
            if (Solutions.Count < totalNoOfSolutions)
                currentProgress = Math.Min(currentProgress, 99);

            // Time-based update: Ensure updates occur at least every configured interval
            var now = DateTime.UtcNow;
            var timeSinceLastUpdate = (now - _lastUpdateTime).TotalSeconds;

            // Extract the condition into a local variable
            var shouldUpdateProgress =
                currentProgress - _lastReported >= SimulationSettings.ProgressThresholdPct ||
                Solutions.Count == totalNoOfSolutions ||
                timeSinceLastUpdate >= SimulationSettings.ProgressIntervalInSeconds;

            // Trigger updates only when the condition is met
            if (shouldUpdateProgress)
            {
                _lastReported = currentProgress;
                _lastUpdateTime = now;
                ProgressValueChanged?.Invoke(this,
                    new ProgressChangedWithTokenEventArgs(currentProgress, _currentSimToken));
            }
        }
    }

    private void NotifySolutionFound()
    {
        if (DisplayMode == DisplayMode.Visualize)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenPositions));
    }

    private void AddSolutionAndNotify()
    {
        var updateDTO = new SolutionUpdateDTO(BoardSize, SolutionMode,
            [.. QueenPositions], Solutions);
        
        _solutionManager.UpdateSolutions(updateDTO);
        Debug.WriteLine($"[AddSolutionAndNotify] Solutions.Count={Solutions.Count}");

        // Report progress after adding a solution
        ReportProgress(ExpectedSolutionCount.GetCount(BoardSize, SolutionMode), SolutionMode);
    }

    private async Task FindAllSolutions(int colIndex, CancellationToken cancellationToken)
    {
        await SolveNQueenForModeAsync(colIndex, SolutionMode.Unique, cancellationToken);

        foreach (var solution in Solutions)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Avoid redundant updates to the solution manager
            var updateDTO = new SolutionUpdateDTO(BoardSize, SolutionMode,
                solution, Solutions);

            _solutionManager.UpdateSolutions(updateDTO);
        }
    }

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
            CleanupResources();
            Solutions?.Clear();
        }
    }

    private void CleanupResources()
    {
        QueenPlaced = null!;
        SolutionFound = null!;
        ProgressValueChanged = null!;
        Solutions?.Clear();
        _cancellationTokenSource?.Dispose();
    }

    #endregion

    private BoardState _board = null!;
    private static readonly HashSet<int[]> _hashSet = [];
    private Guid _currentSimToken = Guid.Empty;
    private double _lastReported = 0;
    private readonly ISolutionManager _solutionManager = solutionManager ??
            throw new ArgumentNullException(nameof(solutionManager));

    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed = false;
    private readonly Func<int, BoardState> _boardStateFactory = boardStateFactory ??
            throw new ArgumentNullException(nameof(boardStateFactory));

    private DateTime _lastUpdateTime = DateTime.MinValue;
}
