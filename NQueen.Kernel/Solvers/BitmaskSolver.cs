using NQueen.Domain.Context;

namespace NQueen.Kernel.Solvers;

public class BitmaskSolver : ISolver, IDisposable
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

        // Sync compatibility booleans from enum configuration
        UseCountOnlyAllMode = AllStorageMode == ResultStorageMode.CountOnly;
        UseCountOnlyUniqueMode = UniqueStorageMode == ResultStorageMode.CountOnly;

        ResetForSolve();
        var sw = Stopwatch.StartNew();

        switch (SolutionMode)
        {
            case SolutionMode.Single:
                SolveSingleMode();
                break;
            case SolutionMode.All:
                if (UseCountOnlyAllMode)
                {
                    SolveAllCountOnlyMode();
                }
                else if (UseParallel)
                {
                    int splitDepth = UseAdaptiveDepth
                        ? ParallelSplitDepthHeuristic.GetOptimalSplitDepth(BoardSize)
                        : ParallelRootSplitDepth;
                    RunAllParallel(splitDepth);
                }
                else
                    RunAllSequential();
                break;
            case SolutionMode.Unique:
                if (UseParallel)
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

    // -------------------- Private Methods & Fields --------------------
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
            // Stop further QueenPlaced / SolutionFound notifications to reduce allocations & GC.
            _eventsSuppressedAfterCap = true;
        }
    }

    private void SolveSingleMode() =>
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                _solutionCount++;
                if (_solutions.Count == 0 && ShouldAddSolution())
                {
                    _solutions.Add((int[])rows.Clone());
                    MaybeSuppressEventsAfterCap();
                }
                return true;
            }
        ));

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var resultSolutions = _solutions
            .Select((sol, idx) => new Solution(sol, _solutionFormatter, idx + 1))
            .ToList();

        // Always return the total number of solutions found, not just the number stored
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(elapsed.TotalSeconds, 1));
    }

    private void RunAllParallel(int splitDepth)
    {
        _parallelEngine.RunAll(new BitmaskParallelEngine.AllRequest(
            BoardSize,
            EnableEvents,
            splitDepth < 1 ? 1 : splitDepth,
            rows =>
            {
                Interlocked.Increment(ref _solutionCount);
                if (ShouldAddSolution())
                {
                    lock (_solutions)
                    {
                        if (ShouldAddSolution())
                        {
                            _solutions.Add(rows);
                            if (ShouldRaiseEvents())
                                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows)));
                            MaybeSuppressEventsAfterCap();
                        }
                    }
                }
            },
            pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
        ));
    }

    private void RunAllSequential()
    {
        // Single-threaded enumeration of all solutions
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                _solutionCount++;
                if (ShouldAddSolution())
                {
                    var copy = (int[])rows.Clone();
                    _solutions.Add(copy);
                    if (ShouldRaiseEvents())
                        SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(copy)));
                    MaybeSuppressEventsAfterCap();
                }
                return false; // keep searching
            }
        ));
    }

    private void SolveAllCountOnlyMode()
    {
        if (UseParallel)
        {
            ulong count = 0;
            _parallelEngine.RunAllCountOnly(new BitmaskParallelEngine.AllCountOnlyRequest(
                BoardSize,
                UseAdaptiveDepth ? -1 : ParallelRootSplitDepth,
                c => count = c,
                pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
            ));
            _solutionCount = count;
            _solutions.Clear();
        }
        else
        {
            ulong count = 0;
            _searchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
                m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    count++;
                    return false;
                }
            ));
            _solutionCount = count;
            _solutions.Clear();
        }
    }

    private void SolveUniqueCountOnlyMode()
    {
        if (UseParallel)
        {
            ulong count = 0;
            _parallelEngine.RunUniqueCountOnly(new BitmaskParallelEngine.UniqueCountOnlyRequest(
                BoardSize,
                1, // Unique mode always uses split depth 1
                c => count = c,
                pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
            ));
            _solutionCount = count;
            _solutions.Clear();
        }
        else
        {
            int N = BoardSize;
            var uniqueKeys = new HashSet<UInt128>();
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
                m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
                rows =>
                {
                    var copy = (int[])rows.Clone();
                    SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratchBuf, out _, out _);
                    return false;
                }
            ));
            _solutionCount = (ulong)uniqueKeys.Count;
            _solutions.Clear();
        }
    }

    private static ulong CountUniqueForRoot(int N, int firstRow)
    {
        ulong count = 0;
        var rowsArr = new int[N];
        Array.Fill(rowsArr, -1);
        rowsArr[0] = firstRow;

        ulong bitFirst = 1UL << firstRow;
        ulong cols = bitFirst;
        ulong d1 = bitFirst << 1;
        ulong d2 = bitFirst >> 1;
        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);

        ulong[] stackCols = new ulong[N];
        ulong[] stackD1 = new ulong[N];
        ulong[] stackD2 = new ulong[N];
        ulong[] stackRemaining = new ulong[N];

        int col = 1;
        ulong remaining = ~(cols | d1 | d2) & mask;

        while (true)
        {
            if (col == N)
            {
                count++;
                col--;
                if (col <= 0) break;
                Restore(col, out remaining);
                continue;
            }
            if (remaining == 0)
            {
                col--;
                if (col <= 0) break;
                Restore(col, out remaining);
                continue;
            }
            ulong bit = remaining & (ulong)-(long)remaining;
            remaining ^= bit;
            int row = BitOperations.TrailingZeroCount(bit);
            rowsArr[col] = row;

            stackCols[col] = cols;
            stackD1[col] = d1;
            stackD2[col] = d2;
            stackRemaining[col] = remaining;

            cols |= bit;
            d1 = (d1 | bit) << 1;
            d2 = (d2 | bit) >> 1;

            col++;
            if (col == N) continue;
            remaining = ~(cols | d1 | d2) & mask;
        }

        void Restore(int c, out ulong rem)
        {
            rem = stackRemaining[c];
            cols = stackCols[c];
            d1 = stackD1[c];
            d2 = stackD2[c];
        }

        return count;
    }

    private void RunUniqueParallel()
    {
        if (UseCountOnlyUniqueMode)
        {
            SolveUniqueCountOnlyMode();
            return;
        }

        _parallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest(
            BoardSize,
            EnableEvents,
            1, // root split depth currently fixed at 1 for Unique mode
            rows =>
            {
                _solutionCount++;
                if (ShouldAddSolution())
                {
                    lock (_solutions)
                    {
                        if (ShouldAddSolution())
                        {
                            _solutions.Add(rows);
                            if (ShouldRaiseEvents())
                                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows)));
                            MaybeSuppressEventsAfterCap();
                        }
                    }
                }
            },
            pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))
        ));
    }

    private void RunUniqueSequential()
    {
        if (UseCountOnlyUniqueMode)
        {
            SolveUniqueCountOnlyMode();
            return;
        }

        int N = BoardSize;
        var uniqueKeys = new HashSet<UInt128>();
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
            m => { if (ShouldRaiseEvents()) QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m)); },
            rows =>
            {
                var copy = (int[])rows.Clone();
                if (SymmetryHelper.AddIfUniquePacked(copy, uniqueKeys, scratchBuf, out _, out var canonical))
                {
                    _solutionCount++;
                    if (ShouldAddSolution())
                    {
                        _solutions.Add(canonical.ToArray());
                        if (ShouldRaiseEvents())
                            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(canonical.ToArray())));
                        MaybeSuppressEventsAfterCap();
                    }
                }
                return false; // continue search
            }
        ));
    }

    private bool ShouldAddSolution()
    {
        if (_capEnabled == false)
            return true;
        var cap = _maxSolutionsInOutput; // use instance max
        return cap <= 0 || _solutions.Count < cap;
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

    private sealed class IntArrayComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[]? x, int[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null || x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
                if (x[i] != y[i]) return false;
            return true;
        }
        public int GetHashCode(int[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var v in obj) hash = hash * 31 + v;
                return hash;
            }
        }
    }
}
