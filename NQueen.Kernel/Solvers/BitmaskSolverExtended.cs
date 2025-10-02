namespace NQueen.Kernel.Solvers;

public class BitmaskSolverExtended(
    ISolutionFormatter solutionFormatter,
    int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
        : ISolver, IDisposable
{
    #region Ctors
    public BitmaskSolverExtended(
        ISolutionFormatter solutionFormatter, bool enableCap)
        : this(solutionFormatter, SimulationSettings.MaxNoOfSolutionsInOutput) =>
            _capEnabled = enableCap;

    public BitmaskSolverExtended(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode,
        ISolutionFormatter solutionFormatter,
        int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
        : this(solutionFormatter, maxSolutionsInOutput)
    {
        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;
    }
    #endregion

    #region IDisposable
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
    #endregion

    #region Events & Public API
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    public int DelayInMillisec { get; set; }

    public int ProgressValue { get; set; }
    
    public int BoardSize { get; private set; }
    
    public SolutionMode SolutionMode { get; private set; }
    
    public DisplayMode DisplayMode { get; private set; }
    
    public bool IsSolverCanceled { get; set; }
    
    public bool EnableParallelization { get; set; } = true;
    
    public bool EnableEvents { get; set; } = true;

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext) =>
        Task.Run(() =>
        {
            BoardSize = simContext.BoardSize;
            SolutionMode = simContext.SolutionMode;
            DisplayMode = simContext.DisplayMode;
            EnableParallelization = simContext.EnableParallelization;
            return Solve();
        });

    public SimulationResults Solve()
    {
        if (BoardSize <= 0)
            throw new InvalidOperationException("BoardSize must be > 0.");

        if (BoardSize > BoardSettings.MaxBitmaskBoardSize)
            throw new NotSupportedException(
                $"Bitmask solver supports boards up to {BoardSettings.MaxBitmaskBoardSize}. (Requested: {BoardSize})");

        ResetForSolve();
        var sw = Stopwatch.StartNew();

        var parallelEligible =
            EnableParallelization &&
            BoardSize >= SimulationSettings.ParallelMinBoardSize &&
            SolutionMode != SolutionMode.Single;

        switch (SolutionMode)
        {
            case SolutionMode.Single:
                SolveSingleMode();
                break;
            
            case SolutionMode.All:
                SolveAllMode(parallelEligible);
                break;
            
            case SolutionMode.Unique:
                SolveUniqueMode(parallelEligible);
                break;
            
            default:
                throw new NotImplementedException($"Unsupported SolutionMode: {SolutionMode}");
        }

        sw.Stop();
        return BuildResults(sw.Elapsed);
    }
    #endregion

    #region Orchestration Helpers
    private void ResetForSolve()
    {
        _solutions.Clear();
        _solutionCount = 0;
        IsSolverCanceled = false;
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
            m =>
            {
                if (EnableEvents)
                    QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m));
            },
            rows =>
            {
                _solutionCount++;
                if (_solutions.Count == 0 && ShouldAddSolution())
                    _solutions.Add((int[])rows.Clone());
                return true;
            }));

    private void SolveAllMode(bool parallelEligible)
    {
        if (parallelEligible)
        {
            RunAllParallel();
            return;
        }
        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: false,
            EnhancedSymmetry: false,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m =>
            {
                if (EnableEvents)
                    QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m));
            },
            rows =>
            {
                _solutionCount++;
                if (ShouldAddSolution())
                    _solutions.Add((int[])rows.Clone());
                return false;
            }));
    }

    private void SolveUniqueMode(bool parallelEligible)
    {
        if (parallelEligible)
        {
            RunUniqueParallel();
            return;
        }
        var uniqueSet = new HashSet<int[]>(new IntArrayComparer());
        var scratchBuf = new int[BoardSize];

        _searchEngine.Run(new BitmaskSearchEngine.Request(
            BoardSize,
            RestrictFirstCol: true,
            EnhancedSymmetry: true,
            DisplayMode,
            DelayInMillisec,
            _currentSimToken,
            () => IsSolverCanceled,
            p => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken)),
            m =>
            {
                if (EnableEvents)
                    QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m));
            },
            rows =>
            {
                var copy = (int[])rows.Clone();
                if (SymmetryHelper.AddIfUnique(copy, uniqueSet, scratchBuf))
                {
                    _solutionCount++;
                    if (ShouldAddSolution())
                        _solutions.Add(copy);
                }
                return false;
            }));
    }

    private SimulationResults BuildResults(TimeSpan elapsed)
    {
        var resultSolutions = _solutions
            .Select((sol, idx) => new Solution(sol, _solutionFormatter, idx + 1))
            .ToList();

        return new SimulationResults(resultSolutions, _solutionCount,
            Math.Round(elapsed.TotalSeconds, 1));
    }
    #endregion

    #region Parallel (Delegated to Engine)
    private void RunAllParallel()
    {
        _parallelEngine.RunAll(new BitmaskParallelEngine.AllRequest(
            BoardSize,
            EnableEvents,
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
                            if (EnableEvents)
                                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows)));
                        }
                    }
                }
            },
            pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))));
    }

    private void RunUniqueParallel()
    {
        _parallelEngine.RunUnique(new BitmaskParallelEngine.UniqueRequest(
            BoardSize,
            EnableEvents,
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
                            if (EnableEvents)
                                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows)));
                        }
                    }
                }
            },
            pct => ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(pct, _currentSimToken))));
    }
    #endregion

    #region Fields / Helpers
    private bool ShouldAddSolution()
    {
        if (_capEnabled == false)
            return true;

        var cap = SimulationSettings.MaxNoOfSolutionsInOutput;

        return cap <= 0 || _solutions.Count < cap;
    }

    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private readonly BitmaskSearchEngine _searchEngine = new();
    private readonly BitmaskParallelEngine _parallelEngine = new();
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private bool _disposed;
    private readonly int _maxSolutionsInOutput = maxSolutionsInOutput;
    #endregion
}
