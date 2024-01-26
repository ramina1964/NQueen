namespace NQueen.Kernel;

public class BackTracking : ISolver
{
    public BackTracking(ISolutionDev solutionDev, sbyte boardSize = Utility.DefaultBoardSize)
    {
        Initialize(boardSize);
        SolutionDev = solutionDev;
    }

    #region ISolverBackEnd

    public bool CancelSolver { get; set; }

    public async Task<SimulationResults> GetResultsAsync(sbyte boardSize,
        SolutionMode solutionMode, DisplayMode displayMode = DisplayMode.Hide)
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

    public void OnProgressChanged(object sender, ProgressValueChangedEventArgs e) =>
        ProgressValueChanged?.Invoke(this, e);

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public HashSet<sbyte[]> Solutions { get; set; }

    public async Task<SimulationResults> GetResultsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var solutions = await MainSolve();
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

    #region PublicProperties

    public ISolutionDev SolutionDev { get; }

    public sbyte BoardSize { get; set; }

    public int NoOfSolutions => Solutions.Count;

    public sbyte HalfSize { get; set; }

    public sbyte[] QueenList { get; set; }

    public int SolutionCountPerUpdate =>
        Utility.SolutionCountPerUpdate(BoardSize);

    #endregion PublicProperties

    protected void OnQueenPlaced(object sender, QueenPlacedEventArgs e) =>
        QueenPlaced?.Invoke(this, e);

    #region PrivateMethods
    private void Initialize(sbyte boardSize = Utility.DefaultBoardSize)
    {
        BoardSize = boardSize;
        CancelSolver = false;
        HalfSize = GetHalfSize();

        QueenList = Enumerable.Repeat((sbyte)-1, BoardSize).ToArray();
        Solutions = new HashSet<sbyte[]>(new SequenceEquality<sbyte>());
    }

    // Todo: Consider renaming this method.
    private async Task<IEnumerable<Solution>> MainSolve()
    {
        // Iterative call to start the simulation
        switch (SolutionMode)
        {
            case SolutionMode.Single:
                await SolveSingle(0);
                break;

            case SolutionMode.Unique:
                await SolveUnique(0);
                break;

            case SolutionMode.All:
                await SolveAll(0);
                break;

            default:
                throw new NotImplementedException();
        }

        return Solutions
               .Select((s, index) => new Solution(s, index + 1));
    }

    private async Task SolveSingle(sbyte colNo)
    {
        while (colNo != -1)
        {
            if (CancelSolver)
            { return; }

            // There is no solution to the problem.
            if (QueenList[0] == HalfSize) return;

            // The solution is found.
            if (colNo == BoardSize)
            {
                var updateDTO = new SolutionUpdateDTO
                {
                    BoardSize = BoardSize,
                    SolutionMode = SolutionMode,
                    Solutions = Solutions,
                    QueenList = QueenList.ToArray()
                };
                SolutionDev.UpdateSolutions(updateDTO);
                UpdateSolutionsInLayout();

                return;
            }

            QueenList[colNo] = PlaceQueen(colNo);

            // The queen can not be placed in this column. Go one column back and try to place it upward on the board.
            if (QueenList[colNo] == -1)
            {
                colNo--;
                continue;
            }

            // A new queen is placed.
            if (DisplayMode == DisplayMode.Visualize)
            {
                OnQueenPlaced(this, new QueenPlacedEventArgs(QueenList));
                await Task.Delay(DelayInMilliseconds);
            }

            colNo++;
        }
    }

    private async Task SolveUnique(sbyte colNo)
    {
        while (colNo != -1)
        {
            if (CancelSolver) return;

            // All solutions are found and registered.
            if (QueenList[0] == HalfSize) return;

            // A new solution is found.
            if (colNo == BoardSize)
            {
                var updateDTO = new SolutionUpdateDTO
                {
                    BoardSize = BoardSize,
                    SolutionMode = SolutionMode,
                    Solutions = Solutions,
                    QueenList = QueenList.ToArray()
                };
                UpdateSolutionsInLayout();
                SolutionDev.UpdateSolutions(updateDTO);

                colNo--;
                continue;
            }

            QueenList[colNo] = PlaceQueen(colNo);

            // The queen can not be placed in this column. Go one column back and try to place it upward in the next iteration.
            if (QueenList[colNo] == -1)
            {
                colNo--;
                continue;
            }

            // A new queen is placed.
            if (DisplayMode == DisplayMode.Visualize)
            {
                OnQueenPlaced(this, new QueenPlacedEventArgs(QueenList));
                await Task.Delay(DelayInMilliseconds);
            }

            colNo++;
        }
    }

    private async Task SolveAll(sbyte colNo)
    {
        // First, find all unique solutions
        await SolveUnique(colNo);

        // Add the current solution and all of its counterparts to Solutions.
        foreach (var solution in Solutions.ToList())
        {
            var updateDTO = new SolutionUpdateDTO
            {
                BoardSize = BoardSize,
                SolutionMode = SolutionMode,
                Solutions = Solutions,
                QueenList = [.. solution]
            };

            SolutionDev.UpdateSolutions(updateDTO);
        }
    }

    private void UpdateSolutionsInLayout()
    {
        if (NoOfSolutions % SolutionCountPerUpdate == 0) UpdateProgressBar();

        // Activate this code in case of IsVisulaized == true.
        if (DisplayMode == DisplayMode.Visualize) SolutionFound(this, new SolutionFoundEventArgs(QueenList));
    }

    // Return the first available row for the queen in column "colNo", -1 if impossible.
    private sbyte PlaceQueen(sbyte colNo)
    {
        colNo = (sbyte)Math.Min(colNo, BoardSize - 1);
        for (sbyte pos = (sbyte)(QueenList[colNo] + 1); pos < BoardSize; pos++)
        {
            var isValid = true;
            for (int j = 0; j < colNo; j++)
            {
                int lhs = Math.Abs(pos - QueenList[j]);
                int rhs = Math.Abs(colNo - j);
                if (0 != lhs && lhs != rhs) continue;

                isValid = false;
                break;
            }

            if (isValid) return pos;
        }

        return -1;
    }

    private void UpdateProgressBar()
    {
        ProgressValue = Math.Round(100.0 * QueenList[0] / HalfSize, 1);
        OnProgressChanged(this, new ProgressValueChangedEventArgs(ProgressValue));
    }

    public sbyte GetHalfSize() =>
        (sbyte)(BoardSize % 2 == 0
        ? BoardSize / 2
        : BoardSize / 2 + 1);

    #endregion PrivateMethods
}
