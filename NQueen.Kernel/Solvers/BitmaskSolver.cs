namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver(ISolutionFormatter solutionFormatter,
    int maxDisplayed = SimulationSettings.MaxDisplayedCount) : ISolver, IDisposable
{
    // Central validation for all solver paths
    private bool ValidateRows(int[] rows)
    {
        // Empty array is used as a sentinel from parallel engines to indicate a count-only solution
        if (rows.Length == 0)
            return true; // accept silently (do not assert / materialize)
        bool ok = rows.Length == BoardSize;
        Debug.Assert(ok, $"[BitmaskSolver] Invalid solution rows length={rows.Length}, BoardSize={BoardSize}");
        return ok;
    }

    public BitmaskSolver(ISolutionFormatter solutionFormatter, bool enableCap)
    : this(solutionFormatter, SimulationSettings.MaxDisplayedCount) =>
    _capEnabled = enableCap;

    public BitmaskSolver(int boardSize, SolutionMode solutionMode, DisplayMode displayMode, ISolutionFormatter solutionFormatter, int maxSolutionsInOutput = SimulationSettings.MaxDisplayedCount)
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

    public bool EnableEvents { get; set; } = true;

    public ResultStorageMode AllStorageMode { get; set; } = SimulationSettings.DefaultAllStorageMode;
    public ResultStorageMode UniqueStorageMode { get; set; } = SimulationSettings.DefaultUniqueStorageMode;

    public bool UseCountOnlyUniqueMode { get; set; } = false;
    public bool UseCountOnlyAllMode { get; set; } = false;

    public bool UseParallel { get; set; } = true;

    public int ParallelRootSplitDepth { get; set; } = 1;

    public bool UseAdaptiveDepth { get; set; } = false;

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
            throw new InvalidOperationException("BoardSize must be >0.");
        if (BoardSize > BoardSettings.MaxBitmaskBoardSize)
            throw new NotSupportedException($"Bitmask solver supports boards up to {BoardSettings.MaxBitmaskBoardSize}. (Requested: {BoardSize})");

        bool allCountOnly = UseCountOnlyAllMode || AllStorageMode == ResultStorageMode.CountOnly;
        bool uniqueCountOnly = UseCountOnlyUniqueMode || UniqueStorageMode == ResultStorageMode.CountOnly;

        ResetForSolve();
        var sw = Stopwatch.StartNew();

        // NOTE: Previous refactor added an early short-circuit for Unique materialization which suppressed solution samples.
        // That has been removed; we now always enumerate up to the cap and then set the authoritative total afterwards.

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
                    bool autoParallel = ParallelSplitDepthHeuristic.ShouldUseParallelForAll(BoardSize);
                    int splitDepth = UseAdaptiveDepth ? ParallelSplitDepthHeuristic.GetOptimalSplitDepth(BoardSize) : ParallelRootSplitDepth;
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
        _rawSolutions = null;
    }

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var cap = (_capEnabled ? _maxDisplayed : 0);
        var resultSolutions = new List<Solution>(_solutions.Count);
        int idx = 1;
        foreach (var (packed, boardSize) in (cap > 0 && _solutions.Count > cap ? _solutions.Take(cap) : _solutions))
        {
            if (boardSize <= 0) continue; // skip malformed entries
            Debug.Assert(boardSize > 0, "Invariant violated: boardSize should be >0 when constructing results.");
            if (_rawSolutions != null && _rawSolutions.Count >= idx)
            {
                var raw = _rawSolutions[idx - 1];
                if (raw != null && raw.Length == boardSize)
                {
                    resultSolutions.Add(new NQueen.Domain.Models.Solution(raw, _formatter, idx));
                }
                else if (raw != null && raw.Length > 0)
                {
                    // fallback: ignore mismatch and still add packed if available
                    resultSolutions.Add(new NQueen.Domain.Models.Solution(packed, boardSize, _formatter, idx));
                }
            }
            else
            {
                resultSolutions.Add(new Solution(packed, boardSize, _formatter, idx));
            }
            idx++;
        }
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private bool ShouldAddSolution()
    {
        if (_capEnabled == false)
            return true;

        return _maxDisplayed <= 0 || _solutions.Count < _maxDisplayed;
    }

    // Helper methods for partial accessibility
    private void SolveUniqueCountOnlyMode()
    {
        // Unified implementation: always use symmetry-aware unique counter.
        _solutionCount = UniqueSolutionCounter.Count(BoardSize, null, _currentSimToken, ProgressValueChanged, this);
        _solutions.Clear();
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void IncrementSolutionCountAtomic() =>
    Interlocked.Increment(ref Unsafe.As<ulong, long>(ref _solutionCount));

    internal static int EstimateUniqueSolutionCount(int boardSize)
    {
        ulong count = ExpectedSolutionCounts.GetUnique(boardSize);
        if (count == 0) return 1_000_000;
        return count > int.MaxValue ? int.MaxValue : (int)count;
    }

    // -------------------- Private Fields --------------------
    private readonly ISolutionFormatter _formatter = solutionFormatter;
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly BitmaskSearchEngine _searchEngine = new();
    private readonly BitmaskParallelEngine _parallelEngine = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private bool _disposed;
    private readonly int _maxDisplayed = maxDisplayed;
    private volatile bool _eventsSuppressedAfterCap;
    private List<int[]>? _rawSolutions;
}
