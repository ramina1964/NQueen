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

    public int DelayInMilliseconds { get; set; }

    public double ProgressValue { get; set; }

    public int BoardSize => _board.BoardSize;

    public int[] QueenPositions { get; private set; } = [];

    public int NoOfSolutions => Solutions.Count;

    public int HalfBoardSize => _board.HalfBoardSize;

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public HashSet<int[]> Solutions { get; private set; } =
        new HashSet<int[]>(new IntArrayComparer());

    public event EventHandler<QueenPlacedEventArgs>
        QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound
        = delegate { };

    public event EventHandler<ProgressValueChangedWithTokenEventArgs>
        ProgressValueChanged = delegate { };

    public void SetSimulationToken(Guid token) => _currentSimulationToken = token;

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        // Offload to background thread to keep UI responsive
        return await Task.Run(() =>
            GetResultsForCurrentConfigurationAsync(_cancellationTokenSource.Token)
        );
    }

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await SolveNQueenProblem(cancellationToken);
        stopwatch.Stop();
        var elapsedTimeInSec = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);

        return new SimulationResults(solutions)
        {
            BoardSize = BoardSize,
            Solutions = solutions,
            NoOfSolutions = solutions.Count(),
            ElapsedTimeInSec = elapsedTimeInSec
        };
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
        _lastReportedPercent = -1;
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

        return Solutions.Select((s, index) => new Solution(s, index + 1));
    }

    private async Task SolveNQueenForModeAsync(int colIndex, SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        var context = new SolverContext(BoardSize, HalfBoardSize,
            ExpectedSolutionCount.Count(BoardSize, SolutionMode));

        while (colIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (QueenPositions[0] == context.HalfBoardSize)
                return;

            if (colIndex == context.BoardSize)
            {
                AddSolutionAndNotify();
                NotifySolutionFound();

                if (solutionMode == SolutionMode.Single)
                    return;

                colIndex--;
                continue;
            }

            var nextRow = await FindQueenPositionAsync(colIndex, cancellationToken);
            if (nextRow == -1)
            {
                QueenPositions[colIndex] = -1;
                colIndex--;
                continue;
            }

            QueenPositions[colIndex] = nextRow;
            if (DisplayMode == DisplayMode.Visualize)
            {
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions));
                ReportProgress(context.ExpectedCount);
            }

            colIndex++;
        }

        // Ensure final progress update after all solutions are processed
        ReportProgress(context.ExpectedCount);
    }

    private void ReportProgress(int totalNoOfSolutions)
    {
        if (totalNoOfSolutions > 0)
        {
            ProgressValue = Math.Clamp(Solutions.Count / (double)totalNoOfSolutions, 0.0, 1.0);
            Debug.WriteLine($"[ReportProgress] Progress: {Solutions.Count}/{totalNoOfSolutions} = {ProgressValue}");
            Debug.WriteLine($"[ReportProgress] Raising ProgressValueChanged: ProgressValue={ProgressValue}, _currentSimulationToken={_currentSimulationToken}");

            Debug.WriteLine($"[AddSolutionAndNotify] BoardSize={BoardSize}, SolutionMode={SolutionMode}, ExpectedTotal={ExpectedSolutionCount.Count(BoardSize, SolutionMode)}, Solutions.Count={Solutions.Count}");
            ProgressValueChanged?.Invoke(this, new ProgressValueChangedWithTokenEventArgs(
                    ProgressValue, _currentSimulationToken));
        }
    }

    private void NotifySolutionFound()
    {
        if (DisplayMode == DisplayMode.Visualize)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenPositions));
    }

    private void AddSolutionAndNotify()
    {
        var updateDTO = new SolutionUpdateDTO
        {
            BoardSize = BoardSize,
            SolutionMode = SolutionMode,
            Solutions = Solutions,
            QueenPositions = (int[])QueenPositions.Clone()
        };
        _solutionManager.UpdateSolutions(updateDTO);

        var totalSolutions = ExpectedSolutionCount.Count(BoardSize, SolutionMode);
        var progressStepPercent = SimulationSettings.ProgressStepPercent;
        if (totalSolutions > 0)
        {
            var percent = (int)(Solutions.Count * 100.0 / totalSolutions);
            if (percent / progressStepPercent > _lastReportedPercent / progressStepPercent)
            {
                _lastReportedPercent = percent;
                ReportProgress(totalSolutions);
            }
        }
    }

    private async Task FindAllSolutions(int colIndex, CancellationToken cancellationToken)
    {
        await SolveNQueenForModeAsync(colIndex, SolutionMode.Unique, cancellationToken);

        foreach (var solution in Solutions)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var updateDTO = new SolutionUpdateDTO
            {
                BoardSize = BoardSize,
                SolutionMode = SolutionMode,
                Solutions = Solutions,
                QueenPositions = solution
            };

            _solutionManager.UpdateSolutions(updateDTO);
        }
    }

    private async Task<int> FindQueenPositionAsync(int colIndex, CancellationToken cancellationToken)
    {
        var minColIndex = Math.Min(colIndex, BoardSize - 1);
        for (var rowIndex = QueenPositions[minColIndex] + 1; rowIndex < BoardSize; rowIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
                return -1;

            if (_board.IsValidPosition(minColIndex, rowIndex))
            {
                if (DisplayMode == DisplayMode.Visualize && DelayInMilliseconds > 0)
                    await Task.Delay(DelayInMilliseconds, cancellationToken);

                return rowIndex;
            }
        }

        return -1;
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

    #endregion IDisposable Implementation

    private BoardState _board = null!;

    private readonly ISolutionManager _solutionManager = solutionManager ??
            throw new ArgumentNullException(nameof(solutionManager));

    private CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed = false;

    private readonly Func<int, BoardState> _boardStateFactory = boardStateFactory ??
            throw new ArgumentNullException(nameof(boardStateFactory));

    private Guid _currentSimulationToken = Guid.Empty;
    private int _lastReportedPercent = -1;
}
