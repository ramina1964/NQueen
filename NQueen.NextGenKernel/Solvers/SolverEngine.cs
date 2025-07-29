namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine : ISolver, IDisposable
{
    public SolverEngine(
        Func<int, BoardState> boardStateFactory,
        ISolutionManager solutionManager)
    {
        _boardStateFactory = boardStateFactory ??
            throw new ArgumentNullException(nameof(boardStateFactory));

        _solutionManager = solutionManager ??
            throw new ArgumentNullException(nameof(solutionManager));
    }

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

    public int SolutionsPerUpdate => SimulationSettings.SolutionCountPerUpdate(BoardSize);

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    private static readonly HashSet<int[]> _hashSet = [];

    public HashSet<int[]> Solutions { get; private set; } = _hashSet;

    public event EventHandler<QueenPlacedEventArgs>
        QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound
        = delegate { };

    public event EventHandler<ProgressValueChangedWithTokenEventArgs>
        ProgressValueChanged = delegate { };

    private Guid _currentSimulationToken = Guid.Empty;
    public void SetSimulationToken(Guid token) => _currentSimulationToken = token;

    public async Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        return await Task.Run(GetResultsForCurrentConfigurationAsync);
    }

    public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await SolveNQueenProblem();
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

    #region Public Methods

    public int GetHalfSize() => _board.HalfBoardSize;

    #endregion

    #region Private Methods

    private void Initialize(int boardSize)
    {
        _board = _boardStateFactory(boardSize);
        _board.Reset();
        QueenPositions = _board.QueenPositions;
        Solutions = new HashSet<int[]>(new IntArrayComparer());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<IEnumerable<Solution>> SolveNQueenProblem()
    {
        switch (SolutionMode)
        {
            case SolutionMode.Single:
                await SolveNQueenForModeAsync(0, SolutionMode.Single);
                break;
            case SolutionMode.Unique:
                await SolveNQueenForModeAsync(0, SolutionMode.Unique);
                break;
            case SolutionMode.All:
                await FindAllSolutions(0);
                break;
            default:
                throw new NotImplementedException();
        }

        return Solutions.Select((s, index) => new Solution(s, index + 1));
    }

    private async Task SolveNQueenForModeAsync(int colIndex, SolutionMode solutionMode)
    {
        var totalNoOfSolutions =
            NQueenSolutionCounts.GetTotalNumberOfSolutions(BoardSize, SolutionMode);

        while (colIndex != -1)
        {
            if (IsSolverCanceled)
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

            QueenPositions[colIndex] = await FindQueenPositionAsync(colIndex);

            if (QueenPositions[colIndex] == -1)
            {
                colIndex--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
            {
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions));
                ReportProgress(totalNoOfSolutions);
            }

            colIndex++;
        }

        // Ensure final progress update after all solutions are processed
        ReportProgress(totalNoOfSolutions);
    }

    private void ReportProgress(int totalNoOfSolutions)
    {
        if (totalNoOfSolutions > 0)
        {
            ProgressValue = Math.Clamp(Solutions.Count / (double)totalNoOfSolutions, 0.0, 1.0);
            Debug.WriteLine($"[ReportProgress] Progress: {Solutions.Count}/{totalNoOfSolutions} = {ProgressValue}");
            Debug.WriteLine($"[ReportProgress] Raising ProgressValueChanged: ProgressValue={ProgressValue}, _currentSimulationToken={_currentSimulationToken}");

            Debug.WriteLine($"[AddSolutionAndNotify] BoardSize={BoardSize}, SolutionMode={SolutionMode}, ExpectedTotal={NQueenSolutionCounts.GetTotalNumberOfSolutions(BoardSize, SolutionMode)}, Solutions.Count={Solutions.Count}");
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

        // Report progress, every SolutionsPerUpdate solutions
        if (Solutions.Count % SolutionsPerUpdate == 0)
            ReportProgress(NQueenSolutionCounts.GetTotalNumberOfSolutions(BoardSize, SolutionMode));
    }

    private async Task FindAllSolutions(int colIndex)
    {
        await SolveNQueenForModeAsync(colIndex, SolutionMode.Unique);

        foreach (var solution in Solutions.ToList())
        {
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

    private async Task<int> FindQueenPositionAsync(int colIndex)
    {
        colIndex = Math.Min(colIndex, BoardSize - 1);
        for (var pos = QueenPositions[colIndex] + 1; pos < BoardSize; pos++)
        {
            if (_board.IsValidPosition(colIndex, pos))
            {
                if (DisplayMode == DisplayMode.Visualize && DelayInMilliseconds > 0)
                    await Task.Delay(DelayInMilliseconds);

                return pos;
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

    #endregion

    private readonly ISolutionManager _solutionManager;
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed = false;
    private readonly Func<int, BoardState> _boardStateFactory;
    private BoardState _board = null!;
}
