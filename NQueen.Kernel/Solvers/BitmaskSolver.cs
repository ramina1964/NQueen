namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver(ISolutionFormatter solutionFormatter,
    int maxDisplayedCount = SimulationSettings.MaxDisplayedCount) : ISolver, IDisposable
{

    public BitmaskSolver(ISolutionFormatter solutionFormatter, bool enableCap)
        : this(solutionFormatter, SimulationSettings.MaxDisplayedCount) => _capEnabled = enableCap;

    public BitmaskSolver(int boardSize, SolutionMode solutionMode, DisplayMode displayMode,
        ISolutionFormatter solutionFormatter, int maxSolutionsInOutput = SimulationSettings.MaxDisplayedCount)
        : this(solutionFormatter, maxSolutionsInOutput)
    {
        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;
        _capEnabled = true;
    }

    private int _delayInMillisec; // enforce min delay via setter below

    // ---------------- Public properties / events ----------------
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    public int DelayInMillisec
    {
        get => _delayInMillisec;
        set => _delayInMillisec = value <= 0 ? 0 : Math.Max(NQueen.Domain.Settings.SimulationSettings.MinDelayInMilliseconds, value);
    }

    public int ProgressValue { get; set; }

    public int BoardSize { get; private set; }

    public SolutionMode SolutionMode { get; private set; }

    public DisplayMode DisplayMode { get; private set; }

    public bool IsSolverCanceled { get; set; }

    /// <summary>When <see langword="true"/> (default), raises <see cref="QueenPlaced"/>,
    /// <see cref="SolutionFound"/> and <see cref="ProgressValueChanged"/> during solving.
    /// Set to <see langword="false"/> in benchmarks or headless runs to eliminate event overhead.</summary>
    public bool EnableEvents { get; set; } = true;

    /// <summary>Controls how All-mode solutions are stored.
    /// <see cref="ResultStorageMode.Materialize"/> collects sample solutions up to the display cap;
    /// <see cref="ResultStorageMode.CountOnly"/> skips allocation entirely.
    /// Overridden at runtime when <see cref="UseCountOnlyAllMode"/> is <see langword="true"/>.</summary>
    public ResultStorageMode AllStorageMode { get; set; } = ResultStorageMode.Materialize;

    /// <summary>Controls how Unique-mode solutions are stored.
    /// <see cref="ResultStorageMode.Materialize"/> collects sample solutions up to the display cap;
    /// <see cref="ResultStorageMode.CountOnly"/> skips allocation entirely.
    /// Overridden at runtime when <see cref="UseCountOnlyUniqueMode"/> is <see langword="true"/>.</summary>
    public ResultStorageMode UniqueStorageMode { get; set; } = ResultStorageMode.Materialize;

    /// <summary>When <see langword="true"/>, Unique mode counts solutions without materialising
    /// any <see cref="Solution"/> objects. Equivalent to setting
    /// <see cref="UniqueStorageMode"/> to <see cref="ResultStorageMode.CountOnly"/>.</summary>
    public bool UseCountOnlyUniqueMode { get; set; } = false;

    /// <summary>When <see langword="true"/>, All mode counts solutions without materialising
    /// any <see cref="Solution"/> objects. Equivalent to setting
    /// <see cref="AllStorageMode"/> to <see cref="ResultStorageMode.CountOnly"/>.</summary>
    public bool UseCountOnlyAllMode { get; set; } = false;

    /// <summary>When <see langword="true"/> (default), the solver dispatches work across all
    /// logical cores via <see cref="System.Threading.Tasks.Parallel"/>.
    /// Set to <see langword="false"/> for reproducible single-threaded measurements.</summary>
    public bool UseParallel { get; set; } = true;

    /// <summary>Number of prefix columns fixed before the work is partitioned across threads.
    /// Higher values create more (smaller) tasks and typically improve load balancing for
    /// large N, at the cost of more task-scheduling overhead for small N.
    /// Ignored when <see cref="UseParallel"/> is <see langword="false"/>.
    /// Default is 1; recommended value for N ≥ 15 is 3.</summary>
    public int ParallelRootSplitDepth { get; set; } = 1;

    /// <summary>When <see langword="true"/>, the solver overrides <see cref="ParallelRootSplitDepth"/>
    /// with a value chosen automatically based on board size and available cores.</summary>
    public bool UseAdaptiveDepth { get; set; } = false;

    /// <summary>Opt #1 — When <see langword="true"/>, prunes prefixes whose canonical form
    /// is lexicographically greater than the current partial solution, eliminating
    /// symmetry-equivalent sub-trees early in the search.</summary>
    public bool EnablePrefixMinimalityPruning { get; set; } = false;

    /// <summary>Opt #14 — When <see langword="true"/>, prunes partial solutions whose
    /// horizontal reflection has already been (or will be) enumerated, halving the
    /// effective search space for boards with reflective symmetry.</summary>
    public bool EnablePartialReflectionPruning { get; set; } = false;

    /// <summary>When <see langword="true"/>, restricts the first-column queen to the top half
    /// of the board and doubles the count, exploiting vertical symmetry.
    /// Valid only for All mode (both CountOnly and Materialize); ignored for Unique and Single.
    /// Recommended for N ≥ 15.</summary>
    public bool EnableHalfBoardRestriction { get; set; } = false;

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext) =>
        Task.Run(() =>
        {
            BoardSize = simContext.BoardSize;
            SolutionMode = simContext.SolutionMode;
            DisplayMode = simContext.DisplayMode;
            return Solve();
        });

    // ---------------- Core Solve ----------------
    public SimulationResults Solve()
    {
        lock (_sync)
        {
            if (BoardSize <= 0)
                throw new InvalidOperationException("BoardSize must be >0.");
            if (BoardSize > BoardSettings.MaxBitmaskBoardSize)
                throw new NotSupportedException($"Bitmask solver supports boards up to {BoardSettings.MaxBitmaskBoardSize}. (Requested: {BoardSize})");
            bool allCountOnly = UseCountOnlyAllMode || AllStorageMode == ResultStorageMode.CountOnly;
            bool uniqueCountOnly = UseCountOnlyUniqueMode || UniqueStorageMode == ResultStorageMode.CountOnly;

            bool origEnableEvents = EnableEvents;
            if (uniqueCountOnly || allCountOnly)
                EnableEvents = false;

            ResetForSolve();
            Solution.ResetSequence();
            var sw = Stopwatch.StartNew();
            bool usedLookup = false;
            switch (SolutionMode)
            {
                case SolutionMode.All:
                    HandleModeCommon(isUnique: false, countOnly: allCountOnly, ref usedLookup);
                    break;
                case SolutionMode.Unique:
                    HandleModeCommon(isUnique: true, countOnly: uniqueCountOnly, ref usedLookup);
                    break;
                case SolutionMode.Single:
                    SolveSingleMode();
                    break;
                default:
                    throw new NotSupportedException($"Unsupported SolutionMode {SolutionMode}");
            }
            sw.Stop();

            // Restore event flag
            EnableEvents = origEnableEvents;

            var results = BuildResults(sw.Elapsed);
            if (usedLookup)
                return new SimulationResults(results.Solutions, _solutionCount, Math.Round(sw.Elapsed.TotalSeconds, 1));
            return results;
        }
    }

    // Unified handler for Unique/All
    private void HandleModeCommon(bool isUnique, bool countOnly, ref bool usedLookup)
    {
        // Attempt lookup first when board size >= global threshold
        if (BoardSize >= NQueen.Domain.Settings.SimulationSettings.LookupThresholdN)
        {
            ulong lookup = isUnique ? ExpectedSolutionCounts.GetUnique(BoardSize) : ExpectedSolutionCounts.GetAll(BoardSize);
            if (lookup > 0)
            {
                _solutionCount = lookup;
                usedLookup = true;
                if (countOnly)
                {
                    ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
                    return; // done
                }
                // Materialize sample solutions using lookup (fast path)
                SampleMaterializeUsingLookup(isUnique);
                return;
            }
        }

        // Below threshold OR lookup unavailable: enumeration path
        if (countOnly)
        {
            if (isUnique)
            {
                _solutionCount = CountUniqueAdaptive(BoardSize);
            }
            else
            {
                // Auto-tune All count-only for performance
                if (BoardSize >= SimulationSettings.ParallelAllMaterializeAutoEnableThresholdN)
                {
                    EnablePrefixMinimalityPruning = false;
                    EnablePartialReflectionPruning = false;
                    UseAdaptiveDepth = true;
                    ParallelRootSplitDepth = 3;
                }

                EnumerateAllAdaptive(countOnly: true);
            }
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        // Materialization path: adaptive enumeration per mode
        if (isUnique)
        {
            if (DisplayMode == DisplayMode.Visualize)
                EnumerateUniqueVisualizeAdaptive();
            else
                ExecuteUniqueModeUnified();
        }
        else
        {
            EnumerateAllAdaptive(countOnly: false);
        }
    }

    // ------------ private fields and helpers ------------
    private readonly ISolutionFormatter _formatter = solutionFormatter ?? throw new ArgumentNullException(nameof(solutionFormatter));
    private readonly List<(UInt128 packed, int boardSize)> _solutions = [];
    private readonly List<int[]> _largeBoardRawSolutions = [];
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private readonly int _maxDisplayedCount = maxDisplayedCount;
    private volatile bool _eventsSuppressedAfterCap;
    private bool _disposed;
    private int[]? _scratchBuffer;

    private void ResetForSolve()
    {
        _solutions.Clear();
        _largeBoardRawSolutions.Clear();
        _solutionCount = 0;
        IsSolverCanceled = false;
        _eventsSuppressedAfterCap = false;
        _scratchBuffer = (_scratchBuffer == null || _scratchBuffer.Length < BoardSize * 8)
            ? new int[BoardSize * 8]
            : _scratchBuffer;
    }

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var cap = (_capEnabled ? _maxDisplayedCount : 0);
        var resultSolutions = new List<Solution>(_solutions.Count + _largeBoardRawSolutions.Count);
        int idx = 1;

        foreach (var tup in (cap > 0 && _solutions.Count > cap ? _solutions.Take(cap) : _solutions))
        {
            var packed = tup.packed;
            var boardSize = tup.boardSize;
            if (boardSize <= 0) continue;
            if (boardSize <= 25)
            {
                resultSolutions.Add(new Solution(packed, boardSize, _formatter, idx));
                idx++;
            }
        }

        foreach (var raw in _largeBoardRawSolutions)
        {
            if (cap > 0 && idx > cap) break;
            resultSolutions.Add(new Solution(raw, _formatter, idx));
            idx++;
        }

        _largeBoardRawSolutions.Clear();
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private bool ValidateRows(int[] rows)
    {
        bool ok = rows.Length == BoardSize;
        Debug.Assert(ok, $"[BitmaskSolver] Invalid solution rows length={rows.Length}, BoardSize={BoardSize}");
        return ok;
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
            _largeBoardRawSolutions.Clear();
            QueenPlaced = null;
            SolutionFound = null;
            ProgressValueChanged = null;
        }
        _disposed = true;
    }

    private readonly Lock _sync = new();

    // Ensures the thread-pool minimum is raised to the available core count at most once
    // per process; repeated calls are no-ops.
    private static int _minThreadsSet = 0;
    private static void EnsureMinThreads()
    {
        if (Interlocked.CompareExchange(ref _minThreadsSet, 1, 0) == 0)
        {
            int cores = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(cores, cores);
        }
    }
}
