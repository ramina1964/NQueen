namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine : ISolver, IDisposable
{
    public SolverEngine(
        ISolutionManager solutionManager,
        ISolutionFormatter solutionFormatter,
        Func<int, BoardState> boardStateFactory)
    {
        _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));
        _boardStateFactory = boardStateFactory ?? throw new ArgumentNullException(nameof(boardStateFactory));
        _solutionFormatter = solutionFormatter ?? throw new ArgumentNullException(nameof(solutionFormatter));
    }

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

    public int NoOfSolutions => Solutions.Count;

    public int HalfBoardSize => _board.HalfBoardSize;

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    public event EventHandler<QueenPlacedEventArgs> QueenPlaced = delegate { };

    public event EventHandler<SolutionFoundEventArgs> SolutionFound = delegate { };

    public event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged = delegate { };

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

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

    //public async Task<SimulationResults> GetResultsForCurrentConfigurationAsync() =>
    //    await GetResultsForCurrentConfigurationAsync(_cancellationTokenSource.Token);

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
                // Stop after finding the first solution
                await SolveNQueenByModeAsync(0, SolutionMode.Single, cancellationToken);
                break;

            case SolutionMode.Unique:
                // Find only unique, non-symmetrical solutions
                await SolveNQueenByModeAsync(0, SolutionMode.Unique, cancellationToken);
                break;

            case SolutionMode.All:
                // Find all solutions, including symmetrical ones
                await FindAllSolutions(0, cancellationToken);
                break;

            default:
                throw new NotImplementedException();
        }

        // Return solutions based on the mode
        var result = new List<Solution>();
        var index = 1;

        foreach (var solution in Solutions)
        {
            // For Single mode, return only the first solution
            if (SolutionMode == SolutionMode.Single && result.Count == 1)
                break;

            result.Add(new Solution(solution, _solutionFormatter, index++));
        }

        return result;
    }

    private async Task SolveNQueenByModeAsync(
        int colIndex, SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        while (colIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (QueenPositions[0] == HalfBoardSize)
                return;

            if (colIndex == BoardSize)
            {
                AddSolutionAndNotify();

                // Terminate early for SolutionMode.Single
                if (solutionMode == SolutionMode.Single)
                    return;

                NotifySolutionFound();
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
    }

    private async Task FindAllSolutions(int colIndex, CancellationToken cancellationToken)
    {
        // Solve for unique solutions first
        await SolveNQueenByModeAsync(colIndex, SolutionMode.Unique, cancellationToken);

        // Temporary list to collect updates
        var updates = new List<SolutionUpdateDTO>();

        // Enumerate the Solutions collection
        foreach (var solution in Solutions)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Collect updates in the temporary list
            var updateDTO = new SolutionUpdateDTO(BoardSize, SolutionMode.All, solution, Solutions);
            updates.Add(updateDTO);
        }

        // Apply updates after enumeration
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

    private readonly ISolutionManager _solutionManager;
    private readonly ISolutionFormatter _solutionFormatter;
    private readonly Func<int, BoardState> _boardStateFactory;

    private BoardState _board = null!;
    private CancellationTokenSource _cancellationTokenSource = new();

    private HashSet<int[]> Solutions { get; set; } = new(new IntArrayComparer());

    // Todo: Find out why this field isn¨t used and fix/remove it.
    private Guid _currentSimToken = Guid.Empty;

    private bool _disposed = false;
}
