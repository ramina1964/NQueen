namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver : ISolver, IDisposable
{
    // -------------------- Public API & Constructors --------------------
    public BitmaskSolver(ISolutionFormatter solutionFormatter, int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
    {
        _solutionFormatter = solutionFormatter;
        _maxSolutionsInOutput = maxSolutionsInOutput;
    }

    public BitmaskSolver(ISolutionFormatter solutionFormatter, bool enableCap)
        : this(solutionFormatter, SimulationSettings.MaxNoOfSolutionsInOutput)
    {
        _capEnabled = enableCap;
    }

    public BitmaskSolver(int boardSize, SolutionMode solutionMode, DisplayMode displayMode, ISolutionFormatter solutionFormatter, int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
        : this(solutionFormatter, maxSolutionsInOutput)
    {
        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;
    }

    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    public int DelayInMillisec { get; set; }
    public int ProgressValue { get; set; }
    public int BoardSize { get; private set; }
    public SolutionMode SolutionMode { get; private set; }
    public DisplayMode DisplayMode { get; private set; }
    public bool IsSolverCanceled { get; set; }
    public bool EnableEvents { get; set; } = true; // External master enable

    // Replace legacy booleans with enum-backed properties
    public ResultStorageMode AllStorageMode { get; set; } = SimulationSettings.DefaultAllStorageMode;
    public ResultStorageMode UniqueStorageMode { get; set; } = SimulationSettings.DefaultUniqueStorageMode;

    // Backward compatibility properties (optional: keep for UI until migrated)
    public bool UseCountOnlyUniqueMode { get; set; } = false;
    public bool UseCountOnlyAllMode { get; set; } = false; // New property for count-only All mode

    // New parallelization tunables
    public bool UseParallel { get; set; } = true; // allow disabling parallel execution (forces single-threaded search)
    public int ParallelRootSplitDepth { get; set; } = 1; // number of leading columns used to create root tasks (>=1). Currently supported for All mode; Unique mode uses 1.
    public bool UseAdaptiveDepth { get; set; } = false; // new property to enable adaptive split depth selection

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext) =>
        Task.Run(() =>
        {
            BoardSize = simContext.BoardSize;
            SolutionMode = simContext.SolutionMode;
            DisplayMode = simContext.DisplayMode;
            return Solve();
        });

    public SimulationResults Solve()
    {
        if (BoardSize <= 0)
            throw new InvalidOperationException("BoardSize must be > 0.");

        if (BoardSize > BoardSettings.MaxBitmaskBoardSize)
            throw new NotSupportedException($"Bitmask solver supports boards up to {BoardSettings.MaxBitmaskBoardSize}. (Requested: {BoardSize})");

        // IMPORTANT: Do NOT overwrite the legacy boolean flags here.
        // Users (e.g. ConsoleApp) may set UseCountOnly* directly. We combine the enum + boolean as an OR.
        bool allCountOnly = UseCountOnlyAllMode || AllStorageMode == ResultStorageMode.CountOnly;
        bool uniqueCountOnly = UseCountOnlyUniqueMode || UniqueStorageMode == ResultStorageMode.CountOnly;

        ResetForSolve();
        var sw = Stopwatch.StartNew();

        switch (SolutionMode)
        {
            case SolutionMode.Single:
                SolveSingleMode();
                break;
            case SolutionMode.All:
                if (allCountOnly)
                {
                    SolveAllCountOnlyMode();
                }
                else
                {
                    // Automatic parallel/sequential selection for All mode
                    bool autoParallel = ParallelSplitDepthHeuristic.ShouldUseParallelForAll(BoardSize);
                    int splitDepth = UseAdaptiveDepth
                        ? ParallelSplitDepthHeuristic.GetOptimalSplitDepth(BoardSize)
                        : ParallelRootSplitDepth;
                    if (autoParallel)
                        RunAllParallel(splitDepth);
                    else
                        RunAllSequential();
                }
                break;
            case SolutionMode.Unique:
                if (uniqueCountOnly)
                {
                    SolveUniqueCountOnlyMode();
                }
                else if (UseParallel)
                    RunUniqueParallel();
                else
                    RunUniqueSequential();
                break;
            default:
                throw new NotImplementedException($"Unsupported SolutionMode: {SolutionMode}");
        }

        sw.Stop();
        return BuildResults(sw.Elapsed);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _solutions.Clear();
            QueenPlaced = null;
            SolutionFound = null;
            ProgressValueChanged = null;
        }
        _disposed = true;
    }

    // -------------------- Shared Helpers (used by partials) --------------------
    private void ResetForSolve()
    {
        _solutions.Clear();
        _solutionCount = 0;
        IsSolverCanceled = false;
        _eventsSuppressedAfterCap = false; // allow events for new run
    }

    private bool ShouldRaiseEvents() => EnableEvents && !_eventsSuppressedAfterCap;

    private void MaybeSuppressEventsAfterCap()
    {
        if (_eventsSuppressedAfterCap) return;
        if (_capEnabled == false) return;
        int cap = _maxSolutionsInOutput; // respect ctor argument
        if (cap > 0 && _solutions.Count >= cap)
        {
            _eventsSuppressedAfterCap = true; // stop further queen / solution events
        }
    }

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var resultSolutions = _solutions
            .Select((sol, idx) => new Solution(sol, _solutionFormatter, idx + 1))
            .ToList();
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private bool ShouldAddSolution()
    {
        if (_capEnabled == false)
            return true;
        var cap = _maxSolutionsInOutput;
        return cap <= 0 || _solutions.Count < cap;
    }

    private void TryStoreSolution(int[] rows, bool clone)
    {
        if (!ShouldAddSolution()) return;
        int[] toStore = clone ? (int[])rows.Clone() : rows;
        _solutions.Add(toStore);
        if (ShouldRaiseEvents())
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(toStore)));
        MaybeSuppressEventsAfterCap();
    }

    // -------------------- Private Fields --------------------
    private readonly ISolutionFormatter _solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private readonly BitmaskSearchEngine _searchEngine = new();
    private readonly BitmaskParallelEngine _parallelEngine = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private bool _disposed;
    private readonly int _maxSolutionsInOutput;
    private volatile bool _eventsSuppressedAfterCap; // dynamic flag to stop event traffic after cap reached
}
