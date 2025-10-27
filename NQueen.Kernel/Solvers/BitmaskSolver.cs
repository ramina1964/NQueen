namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver(ISolutionFormatter solutionFormatter,
    int maxDisplayedCount = SimulationSettings.MaxDisplayedCount) : ISolver, IDisposable
{
    private bool ValidateRows(int[] rows)
    {
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
        switch (SolutionMode)
        {
            case SolutionMode.Single:
                SolveSingleMode();
                break;
            case SolutionMode.All:
                if (allCountOnly)
                    SolveAllCountOnlyMode();
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
                    SolveUniqueCountOnlyMode();
                else if (UseParallel)
                    RunUniqueParallel();
                else
                    RunUniqueSequential();
                _solutionCount = ExpectedSolutionCounts.GetUnique(BoardSize); // authoritative
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
        if (SolutionMode == SolutionMode.Unique && !UseCountOnlyUniqueMode)
        {
            ulong authoritative = ExpectedSolutionCounts.GetUnique(BoardSize);
            if (_solutionCount != authoritative)
                _solutionCount = authoritative;
            if (_rawSolutions != null)
            {
                _rawSolutions = _rawSolutions.Where(arr => arr != null && arr.Length == BoardSize && Array.IndexOf(arr, -1) < 0).ToList();
            }
            if (_solutions.Count > 0)
            {
                _solutions.RemoveAll(s => s.boardSize != BoardSize || s.boardSize <= 0);
            }
        }
        var cap = (_capEnabled ? _maxDisplayedCount : 0);
        var resultSolutions = new List<Solution>(_solutions.Count);
        int idx = 1;
        foreach (var (packed, boardSize) in (cap > 0 && _solutions.Count > cap ? _solutions.Take(cap) : _solutions))
        {
            if (boardSize <= 0) continue;
            if (_rawSolutions != null && _rawSolutions.Count >= idx)
            {
                var raw = _rawSolutions[idx - 1];
                if (raw != null && raw.Length == boardSize && Array.IndexOf(raw, -1) < 0)
                    resultSolutions.Add(new NQueen.Domain.Models.Solution(raw, _formatter, idx));
                else
                    resultSolutions.Add(new NQueen.Domain.Models.Solution(packed, boardSize, _formatter, idx));
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
        if (_capEnabled == false) return true;
        return _maxDisplayedCount <= 0 || _solutions.Count < _maxDisplayedCount;
    }

    private void SolveUniqueCountOnlyMode()
    {
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

    private readonly ISolutionFormatter _formatter = solutionFormatter;
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly BitmaskSearchEngine _searchEngine = new();
    private readonly BitmaskParallelEngine _parallelEngine = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private bool _disposed;
    private readonly int _maxDisplayedCount = maxDisplayedCount;
    private volatile bool _eventsSuppressedAfterCap;
    private List<int[]>? _rawSolutions;

    private void SolveAllCountOnlyMode()
    {
        ulong expectedTotal = ExpectedSolutionCounts.GetAll(BoardSize);
        if (UseParallel)
        {
            ulong count = 0;
            try
            {
                BitmaskParallelEngine.RunAllCountOnly(new BitmaskParallelEngine.AllCountOnlyRequest(
                    BoardSize,
                    UseAdaptiveDepth ? -1 : ParallelRootSplitDepth,
                    c => count = c,
                    pct =>
                    {
                        if (EnableEvents && expectedTotal == 0)
                            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                    }
                ));
            }
            catch (AggregateException ae)
            {
                var first = ae.Flatten().InnerExceptions.FirstOrDefault();
                throw first ?? ae;
            }
            _solutionCount = count;
            _solutions.Clear();
            if (EnableEvents && expectedTotal > 0)
            {
                double pct = expectedTotal == 0 ? 100.0 : Math.Min(100.0, (double)count / expectedTotal * 100.0);
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
            }
        }
        else
        {
            ulong count = 0;
            int lastPct = -1;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => { if (EnableEvents && expectedTotal == 0) ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)); },
                m => { if (EnableEvents && !_eventsSuppressedAfterCap) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize)); },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    count++;
                    if (EnableEvents && expectedTotal > 0)
                    {
                        int pct = (int)Math.Min(100.0, (double)count / expectedTotal * 100.0);
                        if (pct != lastPct)
                        {
                            lastPct = pct;
                            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken));
                        }
                    }
                    return false;
                }
            ));
            _solutionCount = count;
            _solutions.Clear();
            if (EnableEvents && expectedTotal > 0 && lastPct < 100)
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
        }
    }
}
