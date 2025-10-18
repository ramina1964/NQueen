namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver(ISolutionFormatter solutionFormatter,
    int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput) : ISolver, IDisposable
{
    public BitmaskSolver(ISolutionFormatter solutionFormatter, bool enableCap)
        : this(solutionFormatter, SimulationSettings.MaxNoOfSolutionsInOutput) =>
        _capEnabled = enableCap;

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
        _eventsSuppressedAfterCap = false;
        _rawSolutions = null; // ensure no stale arrays from previous run
    }

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var cap = (_capEnabled ? _maxSolutionsInOutput : 0);
        var resultSolutions = new List<NQueen.Domain.Models.Solution>(_solutions.Count);
        int idx = 1;
        foreach (var (packed, boardSize) in (cap > 0 && _solutions.Count > cap ? _solutions.Take(cap) : _solutions))
        {
            // Prefer array materialization when raw arrays are stored (ensures exact board layout, needed for unit tests)
            if (_rawSolutions != null && _rawSolutions.Count >= idx)
            {
                resultSolutions.Add(new NQueen.Domain.Models.Solution(_rawSolutions[idx - 1], _solutionFormatter, idx));
            }
            else
            {
                resultSolutions.Add(new NQueen.Domain.Models.Solution(packed, boardSize, _solutionFormatter, idx));
            }
            idx++;
        }
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private bool ShouldAddSolution()
    {
        if (_capEnabled == false)
            return true;
        var cap = _maxSolutionsInOutput;
        return cap <= 0 || _solutions.Count < cap;
    }

    // Helper methods for partial accessibility
    private void SolveUniqueCountOnlyMode()
    {
        if (UseParallel)
        {
            _solutionCount = UniqueSolutionCounter.Count(BoardSize, null, _currentSimToken, ProgressValueChanged, this);
            _solutions.Clear();
        }
        else
        {
            int N = BoardSize;
            int estimatedUnique = EstimateUniqueSolutionCount(N);
            var uniqueKeys = new HashSet<UInt128>(estimatedUnique);
            var scratchBuf = new int[SymmetryHelper.GetScratchBufferSize(N)];

            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: true,
                EnhancedSymmetry: true,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    var key = SymmetryHelper.GetCanonicalKey(rows, scratchBuf, out _);
                    uniqueKeys.Add(key);
                    return false;
                }
            ));
            _solutionCount = (ulong)uniqueKeys.Count;
            _solutions.Clear();
        }
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void IncrementSolutionCountAtomic() =>
        Interlocked.Increment(ref Unsafe.As<ulong, long>(ref _solutionCount));

    private static int EstimateUniqueSolutionCount(int boardSize)
    {
        return boardSize switch
        {
            12 => 14200,
            13 => 73712,
            14 => 365596,
            15 => 2279184,
            16 => 14772512,
            _ => 1000000
        };
    }

    // -------------------- Private Fields --------------------
    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter;
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly BitmaskSearchEngine _searchEngine = new();
    private readonly BitmaskParallelEngine _parallelEngine = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private bool _disposed;
    private readonly int _maxSolutionsInOutput = maxSolutionsInOutput;
    private volatile bool _eventsSuppressedAfterCap;
    private List<int[]>? _rawSolutions; // added raw solutions storage field
}
