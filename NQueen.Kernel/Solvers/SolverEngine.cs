namespace NQueen.Kernel.Solvers;

// Todo: In all core classes, avoid using Task.FromResult in a loop;
// consider making the method synchronous or using ValueTask<int> for better performance.

public class SolverEngine(
    ISolutionFormatter solutionFormatter,
    Func<int, BoardState> boardStateFactory) : ISolverPruning, IDisposable
{
    // Properties
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

    public Memory<int> QueenPositions { get; private set; } = Memory<int>.Empty;

    public int NoOfSolutions => Solutions.Count;

    public SolutionMode SolutionMode { get; set; }

    public DisplayMode DisplayMode { get; set; }

    // Events
    public event EventHandler<Domain.EventArgsPruning.QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<Domain.EventArgsPruning.SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<Domain.EventArgsPruning.ProgressUpdateEventArgs>? ProgressValueChanged;

    // Public Methods
    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public void CancelSimulation() => _cancellationTokenSource?.Cancel();

    public async Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
    {
        Initialize(simContext.BoardSize);
        SolutionMode = simContext.SolutionMode;
        DisplayMode = simContext.DisplayMode;

        return await RunSimulationAsync(simContext.SolutionMode,
            _cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Private Methods
    private void Initialize(int boardSize)
    {
        _board = _boardStateFactory(boardSize);
        _board.Reset();
        QueenPositions = _board.QueenPositions;
        Solutions = new HashSet<Memory<int>>(new MemoryIntArrayComparer());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task<SimulationResults> RunSimulationAsync(
        SolutionMode solutionMode, CancellationToken cancellationToken)
    {
        var elapsedTime = await MeasureExecutionTimeAsync(async () =>
            await SolveNQueenProblemAsync(solutionMode, cancellationToken));

        // Log the contents of the Solutions collection
        Debug.WriteLine("Contents of Solutions:");
        foreach (var solution in Solutions)
        {
            Debug.WriteLine(string.Join(",", solution.Span.ToArray()));
        }

        // Validate solutions before creating Solution objects
        var validSolutions = Solutions.Where(s =>
        {
            var span = s.Span;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] < 0)
                {
                    Debug.WriteLine($"Invalid solution detected during validation:" +
                        $"{string.Join(",", span.ToArray())}");

                    return false;
                }
            }
            return true;
        });

        // Log the total number of valid solutions
        Debug.WriteLine($"Total number of solutions found: {validSolutions.Count()}");

        return new SimulationResults(
            [.. validSolutions.Select(s => new Solution(s.Span.ToArray(), _solutionFormatter))], elapsedTime);
    }

    private async Task SolveNQueenProblemAsync(SolutionMode solutionMode,
        CancellationToken cancellationToken)
    {
        var simContext = new SimulationContext
        (
            BoardSize,
            solutionMode,
            DisplayMode
        );

        await SolveByModeAsync(simContext, 0, cancellationToken);
    }

    private async Task SolveByModeAsync(SimulationContext simContext,
        int columnIndex, CancellationToken cancellationToken)
    {
        if (simContext.SolutionMode == SolutionMode.Single)
            NotifyProgress(-1);

        while (columnIndex != -1)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                NotifyProgress(0);
                return;
            }

            // For 'Unique Solutions', restrict the first column to HalfBoardSize
            var maxRow = (simContext.SolutionMode == SolutionMode.Unique && columnIndex == 0)
                ? _board.HalfBoardSize
                : BoardSize;

            if (columnIndex == BoardSize)
            {
                // Validate the solution before adding it
                if (ValidationHelper.AreAllPositionsValid(QueenPositions.Span))
                {
                    AddSolutionAndNotify();
                    if (simContext.SolutionMode == SolutionMode.Single)
                        return;
                }
                else
                {
                    Debug.WriteLine($"Invalid solution detected: {string.Join(",", QueenPositions.Span.ToArray())}");
                }

                columnIndex--;
                continue;
            }

            var nextRow = await FindNextRowAsync(columnIndex, maxRow, cancellationToken);

            if (nextRow == -1)
            {
                // Reset the current column's position during backtracking
                QueenPositions.Span[columnIndex] = -1;
                columnIndex--;
                continue;
            }

            QueenPositions.Span[columnIndex] = nextRow;

            if (simContext.DisplayMode == DisplayMode.Visualize)
                NotifyQueenPlaced();

            columnIndex++;
        }
    }

    private async ValueTask<int> FindNextRowAsync(int columnIndex, int maxRow, CancellationToken cancellationToken)
    {
        var currentRow = QueenPositions.Span[columnIndex];
        var startRow = currentRow == -1 ? 0 : currentRow + 1;

        for (var row = startRow; row < maxRow; row++)
        {
            if (cancellationToken.IsCancellationRequested)
                return -1;

            if (SolutionMode == SolutionMode.Unique && columnIndex == 0 && row >= _board.HalfBoardSize)
                continue;

            if (BoardState.IsPositionValid(columnIndex, row, QueenPositions))
            {
                if (DisplayMode == DisplayMode.Visualize && DelayInMillisec > 0)
                    await Task.Delay(DelayInMillisec, cancellationToken);
                return row;
            }
        }

        return -1;
    }

    private void AddSolutionAndNotify()
    {
        // Validate QueenPositions before adding to Solutions
        if (ValidationHelper.AreAllPositionsValid(QueenPositions.Span) == false)
        {
            Debug.WriteLine($"Invalid solution detected: {string.Join(",", QueenPositions.Span.ToArray())}");
            return; // Skip adding invalid solutions
        }

        // Create a copy of QueenPositions to avoid reference issues
        var solutionCopy = new Memory<int>(QueenPositions.ToArray());

        // Check for symmetry if in Unique mode
        if (SolutionMode == SolutionMode.Unique &&
            SymmetryHelper.IsSymmetricalSolution(solutionCopy, Solutions))
        {
            Debug.WriteLine($"Symmetrical solution skipped: {string.Join(",", solutionCopy.Span.ToArray())}");
            return;
        }

        // Log the solution being added
        Debug.WriteLine($"Attempting to add solution: {string.Join(",", solutionCopy.Span.ToArray())}");

        // Add the solution directly since symmetry pruning is handled during solving
        if (!Solutions.Add(solutionCopy))
        {
            Debug.WriteLine($"Solution already exists or failed to add: {string.Join(",", solutionCopy.Span.ToArray())}");
        }
        else
        {
            Debug.WriteLine($"Solution successfully added: {string.Join(",", solutionCopy.Span.ToArray())}");
            SolutionFound?.Invoke(this, new Domain.EventArgsPruning.SolutionFoundEventArgs(solutionCopy.Span.ToArray()));
        }
    }

    private void NotifyProgress(int progress) =>
        ProgressValueChanged?.Invoke(this,
            new Domain.EventArgsPruning.ProgressUpdateEventArgs(progress, _currentSimToken));

    private void NotifyQueenPlaced() =>
        QueenPlaced?.Invoke(this, new Domain.EventArgsPruning.QueenPlacedEventArgs(QueenPositions.Span.ToArray()));

    private static async Task<double> MeasureExecutionTimeAsync(Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            QueenPlaced = null;
            SolutionFound = null;
            ProgressValueChanged = null;
            Solutions?.Clear();
            _cancellationTokenSource?.Dispose();
        }
    }

    // Fields
    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter
        ?? throw new ArgumentNullException(nameof(solutionFormatter));

    private readonly Func<int, BoardState> _boardStateFactory = boardStateFactory
        ?? throw new ArgumentNullException(nameof(boardStateFactory));

    private BoardState _board = null!;
    private CancellationTokenSource _cancellationTokenSource = new();
    private HashSet<Memory<int>> Solutions { get; set; } = new(new MemoryIntArrayComparer());
    private Guid _currentSimToken = Guid.Empty;
    private bool _disposed = false;
}
