namespace NQueen.NextGenKernel.Solvers;

public class SimulationOrchestrator : ISolver, IDisposable
{
    public SimulationOrchestrator(
        ISolutionManager solutionManager,
        int boardSize = BoardSettings.DefaultBoardSize)
    {
        Debug.WriteLine($"[Orchestrator] Created: {GetHashCode()}");

        Initialize(boardSize);
        SolutionManager = solutionManager
            ?? throw new ArgumentNullException(nameof(solutionManager));

        QueenPositions = [.. Enumerable.Repeat(-1, boardSize)];
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void SetSimulationToken(Guid token) =>
        _currentSimulationToken = token;

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

    #region ISolver
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

    public int DelayInMilliseconds { get; set; }

    public double ProgressValue { get; set; }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };
    public event EventHandler<SolutionFoundEventArgs> SolutionFound = delegate { };
    public event EventHandler<ProgressValueChangedWithTokenEventArgs>
        ProgressValueChanged = delegate { };
    #endregion

    #region PublicProperties
    public ISolutionManager SolutionManager { get; }

    public int BoardSize { get; set; }

    public int NoOfSolutions => Solutions.Count;

    public int HalfBoardSize { get; set; }

    public int[] QueenPositions { get; set; }

    public int SolutionPerUpdate =>
        SimulationSettings.SolutionCountPerUpdate(BoardSize);
    #endregion

    #region PublicMethods
    public int GetHalfSize() =>
        BoardSize % 2 == 0 ? BoardSize / 2 : BoardSize / 2 + 1;

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

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public HashSet<int[]> Solutions { get; set; } = [];
    #endregion

    #region PrivateMethods
    private void Initialize(int boardSize = BoardSettings.DefaultBoardSize)
    {
        BoardSize = boardSize;
        _cancellationTokenSource = new CancellationTokenSource();
        HalfBoardSize = GetHalfSize();
        QueenPositions = [.. Enumerable.Repeat(-1, BoardSize)];
        Solutions = new HashSet<int[]>(new IntArrayComparer());
        _solutionsSinceLastProgressUpdate = 0;
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

    private async Task SolveNQueenForModeAsync(int colNo, SolutionMode solutionMode)
    {
        var totalNoOfSolutions =
            NQueenSolutionCounts.GetTotalNumberOfSolutions(BoardSize, solutionMode);

        while (colNo != -1)
        {
            if (IsSolverCanceled)
                return;

            if (QueenPositions[0] == HalfBoardSize)
                return;

            if (colNo == BoardSize && solutionMode == SolutionMode.Single)
            {
                AddSolutionAndNotify();
                NotifySolutionFound();
                return;
            }
            if (colNo == BoardSize && (solutionMode == SolutionMode.Unique || solutionMode == SolutionMode.All))
            {
                AddSolutionAndNotify();
                NotifySolutionFound();

                colNo--;
                continue;
            }

            QueenPositions[colNo] = await FindQueenPositionAsync(colNo);

            if (QueenPositions[colNo] == -1)
            {
                colNo--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
                QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(QueenPositions));


            colNo++;
        }

        // Ensure final progress update after all solutions are processed
        ReportProgress(totalNoOfSolutions);
    }

    // private double _lastReportedProgress = -1;

    private void ReportProgress(int totalNoOfSolutions)
    {
        if (totalNoOfSolutions > 0)
        {
            ProgressValue = Math.Clamp(Solutions.Count / (double)totalNoOfSolutions, 0.0, 1.0);
                Debug.WriteLine($"[ReportProgress] Progress: {Solutions.Count}/{totalNoOfSolutions} = {ProgressValue}");
                Debug.WriteLine($"[ReportProgress] Raising ProgressValueChanged: ProgressValue={ProgressValue}, _currentSimulationToken={_currentSimulationToken}");
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
        SolutionManager.UpdateSolutions(updateDTO);

        _solutionsSinceLastProgressUpdate++;

        // Only report progress every SolutionsPerUpdate solutions
        if (Solutions.Count % SolutionPerUpdate == 0)
            ReportProgress(NQueenSolutionCounts.GetTotalNumberOfSolutions(BoardSize, SolutionMode));
    }

    private async Task FindAllSolutions(int colNo)
    {
        await SolveNQueenForModeAsync(colNo, SolutionMode.Unique);

        foreach (var solution in Solutions.ToList())
        {
            var updateDTO = new SolutionUpdateDTO
            {
                BoardSize = BoardSize,
                SolutionMode = SolutionMode,
                Solutions = Solutions,
                QueenPositions = solution
            };

            SolutionManager.UpdateSolutions(updateDTO);
        }
    }

    private async Task<int> FindQueenPositionAsync(int colNo)
    {
        colNo = Math.Min(colNo, BoardSize - 1);
        for (var pos = QueenPositions[colNo] + 1; pos < BoardSize; pos++)
        {
            if (IsValidPosition(colNo, pos))
            {
                if (DisplayMode == DisplayMode.Visualize && DelayInMilliseconds > 0)
                    await Task.Delay(DelayInMilliseconds);

                return pos;
            }
        }

        return -1;
    }

    private bool IsValidPosition(int colNo, int pos)
    {
        for (var j = 0; j < colNo; j++)
        {
            var lhs = Math.Abs(pos - QueenPositions[j]);
            var rhs = Math.Abs(colNo - j);
            if (lhs == 0 || lhs == rhs)
                return false;
        }
        return true;
    }
    #endregion

    private bool _disposed = false;
    private CancellationTokenSource _cancellationTokenSource;
    private Guid _currentSimulationToken = Guid.Empty;
    private int _solutionsSinceLastProgressUpdate;
}