namespace NQueen.Kernel;

public class BackTrackingSolver : ISolver, IDisposable
{
    public BackTrackingSolver(
        ISolutionManager solutionManager,
        sbyte boardSize = Utility.DefaultBoardSize)
    {
        Initialize(boardSize);
        SolutionManager = solutionManager;
    }

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

        // Here _disposed == true
        _disposed = true;
        if (disposing)
            CleanupResources();
    }

    private void CleanupResources()
    {
        // Unsubscribe event handlers
        QueenPlaced = null;
        SolutionFound = null;
        ProgressValueChanged = null;

        // Clear collections
        Solutions?.Clear();

        // Dispose CancellationToken
        _cancelationTokenSource?.Dispose();
    }
    #endregion IDisposable Implementation

    #region ISolverBackEnd
    public bool IsSolverCanceled
    {
        get => _cancelationTokenSource?.IsCancellationRequested ?? false;
        set
        {
            if (value)
                _cancelationTokenSource?.Cancel();
            else
                _cancelationTokenSource = new CancellationTokenSource();
        }
    }

    public async Task<SimulationResults> GetResultsAsync(
        sbyte boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        return await Task.Run(GetResultsAsync);
    }
    #endregion ISolverBackEnd

    #region ISolverUI
    public int DelayInMilliseconds { get; set; }

    public double ProgressValue { get; set; }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced;

    public event EventHandler<SolutionFoundEventArgs> SolutionFound;

    public event EventHandler<ProgressValueChangedEventArgs> ProgressValueChanged;
    #endregion ISolverUI

    #region PublicProperties
    public ISolutionManager SolutionManager { get; }

    public sbyte BoardSize { get; set; }

    public int NoOfSolutions => Solutions.Count;

    public sbyte HalfBoardSize { get; set; }

    public sbyte[] QueenPositions { get; set; }

    public int SolutionCountPerUpdate =>
        Utility.SolutionCountPerUpdate(BoardSize);
    #endregion PublicProperties

    #region PublicMethods
    public sbyte GetHalfSize() =>
        (sbyte)(BoardSize % 2 == 0 ? BoardSize / 2 : BoardSize / 2 + 1);

    public void OnProgressChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValueChanged?.Invoke(this, e);

    public async Task<SimulationResults> GetResultsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await SolveNQueenProblem();
        stopwatch.Stop();
        var elapsedTimeInSec = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);

        return new SimulationResults(solutions)
        {
            BoardSize = BoardSize,
            Solutions = solutions.ToList(),
            NoOfSolutions = Solutions.Count,
            ElapsedTimeInSec = elapsedTimeInSec
        };
    }

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public HashSet<sbyte[]> Solutions { get; set; }
    #endregion

    #region Protected
    protected void OnQueenPlaced(object sender, QueenPlacedEventArgs e) =>
        QueenPlaced?.Invoke(this, e);
    #endregion Protected

    #region PrivateMethods
    private void Initialize(sbyte boardSize = Utility.DefaultBoardSize)
    {
        BoardSize = boardSize;
        _cancelationTokenSource = new CancellationTokenSource();
        HalfBoardSize = GetHalfSize();
        QueenPositions = Enumerable.Repeat((sbyte)-1, BoardSize).ToArray();
        Solutions = new HashSet<sbyte[]>(new SequenceEquality<sbyte>());
    }

    private async Task<IEnumerable<Solution>> SolveNQueenProblem()
    {
        switch (SolutionMode)
        {
            case SolutionMode.Single:
                await FindSingleOrUniqueSolutions(0, SolutionMode.Single);
                break;

            case SolutionMode.Unique:
                await FindSingleOrUniqueSolutions(0, SolutionMode.Unique);
                break;

            case SolutionMode.All:
                await FindAllSolutions(0);
                break;

            default:
                throw new NotImplementedException();
        }

        return Solutions.Select((s, index) => new Solution(s, index + 1));
    }

    private async Task FindSingleOrUniqueSolutions(sbyte colNo, SolutionMode solutionMode)
    {
        while (colNo != -1)
        {
            if (IsSolverCanceled)
                return;

            if (QueenPositions[0] == HalfBoardSize)
                return;

            if (colNo == BoardSize && solutionMode == SolutionMode.Single)
            {
                UpdateSolutions();
                NotifySolutionFound();
                var updateDTO = new SolutionUpdateDTO
                {
                    BoardSize = BoardSize,
                    SolutionMode = SolutionMode,
                    Solutions = Solutions,
                    QueenPositions = (sbyte[])QueenPositions.Clone()
                };
                SolutionManager.UpdateSolutions(updateDTO);
                return;
            }

            else if (colNo == BoardSize && solutionMode == SolutionMode.Unique)
            {
                UpdateSolutions();
                NotifySolutionFound();

                colNo--;
                continue;
            }

            QueenPositions[colNo] = FindQueenPosition(colNo);

            if (QueenPositions[colNo] == -1)
            {
                colNo--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
            {
                OnQueenPlaced(this, new QueenPlacedEventArgs(QueenPositions));
                await Task.Delay(DelayInMilliseconds);
            }

            colNo++;
        }
    }

    private async Task FindAllSolutions(sbyte colNo)
    {
        await FindSingleOrUniqueSolutions(colNo, SolutionMode.Unique);

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

    private void NotifySolutionFound()
    {
        if (NoOfSolutions % SolutionCountPerUpdate == 0)
            NotifyProgressChanged();

        if (DisplayMode == DisplayMode.Visualize)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenPositions));
    }

    private sbyte FindQueenPosition(sbyte colNo)
    {
        colNo = (sbyte)Math.Min(colNo, BoardSize - 1);
        for (sbyte pos = (sbyte)(QueenPositions[colNo] + 1); pos < BoardSize; pos++)
        {
            if (IsValidPosition(colNo, pos))
                return pos;
        }

        return -1;
    }

    private bool IsValidPosition(sbyte colNo, sbyte pos)
    {
        for (int j = 0; j < colNo; j++)
        {
            int lhs = Math.Abs(pos - QueenPositions[j]);
            int rhs = Math.Abs(colNo - j);
            if (lhs == 0 || lhs == rhs)
                return false;
        }
        return true;
    }

    private void NotifyProgressChanged()
    {
        ProgressValue = Math.Round(100.0 * QueenPositions[0] / HalfBoardSize, 1);
        OnProgressChanged(this, new ProgressValueChangedEventArgs(ProgressValue));
    }

    private void UpdateSolutions()
    {
        var updateDTO = new SolutionUpdateDTO
        {
            BoardSize = BoardSize,
            SolutionMode = SolutionMode,
            Solutions = Solutions,
            QueenPositions = (sbyte[])QueenPositions.Clone()
        };
        SolutionManager.UpdateSolutions(updateDTO);
    }
    #endregion

    private bool _disposed = false;
    private CancellationTokenSource _cancelationTokenSource;
}
