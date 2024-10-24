namespace NQueen.Kernel;

public class BackTracking : ISolver, IDisposable
{
    public BackTracking(
        ISolutionDeveloper solutionDeveloper,
        sbyte boardSize = Utility.DefaultBoardSize)
    {
        Initialize(boardSize);
        SolutionDeveloper = solutionDeveloper;
    }

    #region IDisposable Implementation
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed == false)
        {
            _disposed = true;
            if (disposing)
                CleanupResources();
        }
    }

    private void CleanupResources()
    {
        // Unsubscribe event handlers
        QueenPlaced = null;
        SolutionFound = null;
        ProgressValueChanged = null;

        // Clear collections
        Solutions?.Clear();
    }
    #endregion IDisposable Implementation

    #region ISolverBackEnd
    public bool IsSolverCanceled { get; set; }

    public async Task<SimulationResults> GetResultsAsync(
        sbyte boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        Initialize(boardSize);
        SolutionMode = solutionMode;
        DisplayMode = displayMode;

        var ret = Task.Factory.StartNew(GetResultsAsync);
        return await (await ret);
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
    public ISolutionDeveloper SolutionDeveloper { get; }

    public sbyte BoardSize { get; set; }

    public int NoOfSolutions => Solutions.Count;

    public sbyte HalfBoardSize { get; set; }

    public sbyte[] QueenList { get; set; }

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
        var timeInSec = (double)stopwatch.ElapsedMilliseconds / 1000;
        var elapsedTimeInSec = Math.Round(timeInSec, 1);

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
        IsSolverCanceled = false;
        HalfBoardSize = GetHalfSize();
        QueenList = Enumerable.Repeat((sbyte)-1, BoardSize).ToArray();
        Solutions = new HashSet<sbyte[]>(new SequenceEquality<sbyte>());
    }

    private async Task<IEnumerable<Solution>> SolveNQueenProblem()
    {
        switch (SolutionMode)
        {
            case SolutionMode.Single:
                await FindSingleSolution(0);
                break;
            case SolutionMode.Unique:
                await FindUniqueSolutions(0);
                break;
            case SolutionMode.All:
                await FindAllSolutions(0);
                break;
            default:
                throw new NotImplementedException();
        }

        return Solutions.Select((s, index) => new Solution(s, index + 1));
    }

    private async Task FindSingleSolution(sbyte colNo)
    {
        while (colNo != -1)
        {
            if (IsSolverCanceled) return;

            if (QueenList[0] == HalfBoardSize) return;

            if (colNo == BoardSize)
            {
                UpdateSolutions();
                NotifySolutionFound();
                return;
            }

            QueenList[colNo] = FindQueenPosition(colNo);

            if (QueenList[colNo] == -1)
            {
                colNo--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
            {
                OnQueenPlaced(this, new QueenPlacedEventArgs(QueenList));
                await Task.Delay(DelayInMilliseconds);
            }

            colNo++;
        }
    }

    private async Task FindUniqueSolutions(sbyte colNo)
    {
        while (colNo != -1)
        {
            if (IsSolverCanceled) return;

            if (QueenList[0] == HalfBoardSize) return;

            if (colNo == BoardSize)
            {
                UpdateSolutions();
                NotifySolutionFound();
                colNo--;
                continue;
            }

            QueenList[colNo] = FindQueenPosition(colNo);

            if (QueenList[colNo] == -1)
            {
                colNo--;
                continue;
            }

            if (DisplayMode == DisplayMode.Visualize)
            {
                OnQueenPlaced(this, new QueenPlacedEventArgs(QueenList));
                await Task.Delay(DelayInMilliseconds);
            }

            colNo++;
        }
    }

    private async Task FindAllSolutions(sbyte colNo)
    {
        await FindUniqueSolutions(colNo);

        foreach (var solution in Solutions.ToList())
        {
            var updateDTO = new SolutionUpdateDTO
            {
                BoardSize = BoardSize,
                SolutionMode = SolutionMode,
                Solutions = Solutions,
                QueenPositions = solution
            };

            SolutionDeveloper.UpdateSolutions(updateDTO);
        }
    }

    private void NotifySolutionFound()
    {
        if (NoOfSolutions % SolutionCountPerUpdate == 0)
            NotifyProgressChanged();

        if (DisplayMode == DisplayMode.Visualize)
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(QueenList));
    }

    private sbyte FindQueenPosition(sbyte colNo)
    {
        colNo = (sbyte)Math.Min(colNo, BoardSize - 1);
        for (sbyte pos = (sbyte)(QueenList[colNo] + 1); pos < BoardSize; pos++)
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
            int lhs = Math.Abs(pos - QueenList[j]);
            int rhs = Math.Abs(colNo - j);
            if (lhs == 0 || lhs == rhs)
                return false;
        }
        return true;
    }

    private void NotifyProgressChanged()
    {
        ProgressValue = Math.Round(100.0 * QueenList[0] / HalfBoardSize, 1);
        OnProgressChanged(this, new ProgressValueChangedEventArgs(ProgressValue));
    }

    private void UpdateSolutions()
    {
        var updateDTO = new SolutionUpdateDTO
        {
            BoardSize = BoardSize,
            SolutionMode = SolutionMode,
            Solutions = Solutions,
            QueenPositions = [.. QueenList]
        };
        SolutionDeveloper.UpdateSolutions(updateDTO);
    }
    #endregion

    private bool _disposed = false;
}
