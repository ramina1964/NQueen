namespace NQueen.Kernel.Solvers;

public class BitmaskSolverExtended(
    ISolutionFormatter solutionFormatter,
    int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
        : ISolver, IDisposable
{
    #region Ctors
    public BitmaskSolverExtended(ISolutionFormatter solutionFormatter, bool enableCap)
        : this(solutionFormatter, SimulationSettings.MaxNoOfSolutionsInOutput) => _capEnabled = enableCap;

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
        BitmaskIterative(rows =>
        {
            _solutionCount++;
            if (_solutions.Count == 0 && ShouldAddSolution())
                _solutions.Add((int[])rows.Clone());
            return true;
        }, restrictFirstCol: false, enhancedSymmetry: false);

    private void SolveAllMode(bool parallelEligible)
    {
        if (parallelEligible)
        {
            SolveAllParallel();
            return;
        }
        BitmaskIterative(rows =>
        {
            _solutionCount++;
            if (ShouldAddSolution())
                _solutions.Add((int[])rows.Clone());
            return false;
        }, restrictFirstCol: false, enhancedSymmetry: false);
    }

    private void SolveUniqueMode(bool parallelEligible)
    {
        if (parallelEligible)
        {
            SolveUniqueParallel();
            return;
        }
        var uniqueSet = new HashSet<int[]>(new IntArrayComparer());
        var scratchBuf = new int[BoardSize];
        BitmaskIterative(rows =>
        {
            var copy = (int[])rows.Clone();
            if (SymmetryHelper.AddIfUnique(copy, uniqueSet, scratchBuf))
            {
                _solutionCount++;
                if (ShouldAddSolution())
                    _solutions.Add(copy);
            }
            return false;
        }, restrictFirstCol: true, enhancedSymmetry: true);
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

    #region Parallel Variants
    private void SolveAllParallel()
    {
        int totalRoots = BoardSize; // unrestricted first column
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(0.0, _currentSimToken));
        int rootsCompleted = 0;

        ParallelRootSplit(
            onSolution: rows =>
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
                                SolutionFound?.Invoke(this,
                                    new SolutionFoundEventArgs(new Memory<int>(rows)));
                        }
                    }
                }
                return false;
            },
            restrictFirstCol: false,
            enhancedSymmetry: false,
            unique: false,
            totalRoots: totalRoots,
            onRootCompleted: () =>
            {
                if (!EnableEvents) return;
                int done = Interlocked.Increment(ref rootsCompleted);
                double pct = Math.Min(100.0, (double)done / totalRoots * 100.0);
                ProgressValueChanged?.Invoke(this,
                    new ProgressUpdateEventArgs(pct, _currentSimToken));
            });
    }

    private void SolveUniqueParallel()
    {
        int N = BoardSize;
        int totalRoots = (N + 1) / 2;
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(0.0, _currentSimToken));
        int rootsCompleted = 0;

        var tasks = new List<Task<HashSet<int[]>>>();
        var cancelSource = new CancellationTokenSource();
        var token = cancelSource.Token;

        for (int firstRow = 0; firstRow < totalRoots; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localUnique = new HashSet<int[]>(new IntArrayComparer());
                var scratchBuf = new int[N];
                var rowsArr = new int[N];
                Array.Fill(rowsArr, -1);
                rowsArr[0] = fr;

                ulong bitFirst = 1UL << fr;
                ulong cols = bitFirst;
                ulong d1 = bitFirst << 1;
                ulong d2 = bitFirst >> 1;
                ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);

                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackRemaining = new ulong[N];

                int col = 1;
                ulong remaining = ComputeAvailable(1);

                while (!token.IsCancellationRequested)
                {
                    if (col == N)
                    {
                        // Use scratchBuf for AddIfUnique to avoid repeated allocations
                        if (SymmetryHelper.AddIfUnique(rowsArr, localUnique, scratchBuf)) { }
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
                    remaining = ComputeAvailable(col);
                }

                if (EnableEvents)
                {
                    int done = Interlocked.Increment(ref rootsCompleted);
                    double pct = Math.Min(100.0, (double)done / totalRoots * 100.0);
                    ProgressValueChanged?.Invoke(this,
                        new ProgressUpdateEventArgs(pct, _currentSimToken));
                }

                return localUnique;

                ulong ComputeAvailable(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
                    int maxRow = (c == 1)
                        ? (((N & 1) == 1 && rowsArr[0] == N / 2) ? N / 2 : N)
                        : N;
                    if (maxRow < N)
                        avail &= (1UL << maxRow) - 1UL;
                    return avail;
                }

                void Restore(int c, out ulong rem)
                {
                    rem = stackRemaining[c];
                    cols = stackCols[c];
                    d1 = stackD1[c];
                    d2 = stackD2[c];
                }
            }, token));
        }

        Task.WaitAll(tasks.ToArray());

        var globalUnique = new HashSet<int[]>(new IntArrayComparer());
        var globalScratchBuf = new int[N];
        foreach (var t in tasks)
        {
            foreach (var sol in t.Result)
            {
                // Always increment _solutionCount for every unique solution found
                if (SymmetryHelper.AddIfUnique(sol, globalUnique, globalScratchBuf))
                {
                    _solutionCount++;
                    if (ShouldAddSolution())
                    {
                        _solutions.Add((int[])sol.Clone());
                        if (EnableEvents)
                            SolutionFound?.Invoke(this,
                                new SolutionFoundEventArgs(new Memory<int>(sol)));
                    }
                }
            }
        }

        ProgressValueChanged?.Invoke(this,
            new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void ParallelRootSplit(
        Func<int[], bool> onSolution,
        bool restrictFirstCol,
        bool enhancedSymmetry,
        bool unique = false,
        int? totalRoots = null,
        Action? onRootCompleted = null)
    {
        int N = BoardSize;
        int maxRow0 = restrictFirstCol ? (N + 1) / 2 : N;
        totalRoots ??= maxRow0;

        var tasks = new List<Task>();
        var globalUnique = unique ? new HashSet<string>() : null;

        for (int firstRow = 0; firstRow < maxRow0; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var rowsArr = new int[N];
                Array.Fill(rowsArr, -1);
                rowsArr[0] = fr;

                ulong bitFirst = 1UL << fr;
                ulong cols = bitFirst;
                ulong d1 = bitFirst << 1;
                ulong d2 = bitFirst >> 1;
                ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);

                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackRemaining = new ulong[N];

                var scratchUnique = unique ? new HashSet<int[]>(new IntArrayComparer()) : null;
                var scratchBuf = unique ? new int[N] : null;

                int col = 1;
                ulong remaining = ComputeAvailable(col);

                while (true)
                {
                    if (col == N)
                    {
                        var copy = (int[])rowsArr.Clone();
                        if (unique)
                        {
                            if (SymmetryHelper.AddIfUnique(copy, scratchUnique!, scratchBuf!))
                            {
                                var key = string.Join(',', copy);
                                lock (globalUnique!)
                                {
                                    if (globalUnique.Add(key))
                                        onSolution(copy);
                                }
                            }
                        }
                        else
                        {
                            onSolution(copy);
                        }
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
                    remaining = ComputeAvailable(col);
                }

                onRootCompleted?.Invoke();

                ulong ComputeAvailable(int c)
                {
                    ulong avail = ~(cols | d1 | d2) & mask;
                    int maxRow = N;
                    if (enhancedSymmetry && restrictFirstCol && c == 1)
                    {
                        int first = rowsArr[0];
                        maxRow = ((N & 1) == 1 && first == N / 2) ? N / 2 : N;
                    }
                    if (restrictFirstCol && c == 0)
                        maxRow = maxRow0;
                    if (maxRow < N)
                        avail &= (1UL << maxRow) - 1UL;
                    return avail;
                }

                void Restore(int c, out ulong rem)
                {
                    rem = stackRemaining[c];
                    cols = stackCols[c];
                    d1 = stackD1[c];
                    d2 = stackD2[c];
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
            ProgressValueChanged?.Invoke(this,
            new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }
    #endregion

    #region Core Iterative Solver (Upgraded to ulong)
    private void BitmaskIterative(
        Func<int[], bool> onSolution,
        bool restrictFirstCol = false,
        bool enhancedSymmetry = false)
    {
        int N = BoardSize;
        if (N > BoardSettings.MaxBitmaskBoardSize)
            throw new NotSupportedException($"Max supported board size is {BoardSettings.MaxBitmaskBoardSize}.");

        var queenRows = new int[N];
        Array.Fill(queenRows, -1);

        ulong mask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
        ulong cols = 0;
        ulong diag1 = 0;
        ulong diag2 = 0;

        int maxRow0 = restrictFirstCol ? (N + 1) / 2 : N;

        ulong[] stackCols = new ulong[N];
        ulong[] stackD1 = new ulong[N];
        ulong[] stackD2 = new ulong[N];
        ulong[] stackRemaining = new ulong[N];

        int rootPlacements = 0;
        int rootTotal = restrictFirstCol ? maxRow0 : N;

        ProgressValueChanged?.Invoke(this,
            new ProgressUpdateEventArgs(0.0, _currentSimToken));

        var visualize = DisplayMode == DisplayMode.Visualize;
        var delay = (visualize && DelayInMillisec > 0) ? DelayInMillisec : 0;
        int queenPlacedSampleRate =
            N >= SimulationSettings.QueenPlacedSamplingThresholdSize
                ? SimulationSettings.QueenPlacedLargeBoardSampleRate
                : 1;

        int queenPlacedCounter = 0;
        int lastDepth = -1;

        int col = 0;
        ulong remaining = ComputeAvailable(0);

        while (true)
        {
            if (IsSolverCanceled) break;

            if (col == N)
            {
                if (onSolution(queenRows))
                    break;

                col--;
                if (col < 0) break;
                Restore(col, out remaining);
                continue;
            }

            if (remaining == 0)
            {
                col--;
                if (col < 0) break;
                Restore(col, out remaining);
                continue;
            }

            ulong bit = remaining & (ulong)-(long)remaining;
            remaining ^= bit;
            int row = BitOperations.TrailingZeroCount(bit);
            queenRows[col] = row;

            if (col == 0)
            {
                rootPlacements++;
                var pct = (double)rootPlacements / rootTotal * 100.0;
                ProgressValueChanged?.Invoke(this,
                    new ProgressUpdateEventArgs(pct, _currentSimToken));
            }

            if (visualize)
            {
                queenPlacedCounter++;
                if (queenPlacedCounter % queenPlacedSampleRate == 0 || col > lastDepth)
                {
                    QueenPlaced?.Invoke(this,
                        new QueenPlacedEventArgs(new Memory<int>(queenRows)));
                    lastDepth = col;
                }
                if (delay > 0)
                    Thread.Sleep(delay);
            }

            stackCols[col] = cols;
            stackD1[col] = diag1;
            stackD2[col] = diag2;
            stackRemaining[col] = remaining;

            cols |= bit;
            diag1 = (diag1 | bit) << 1;
            diag2 = (diag2 | bit) >> 1;

            col++;
            if (col == N) continue;
            remaining = ComputeAvailable(col);
        }

        ProgressValueChanged?.Invoke(this,
            new ProgressUpdateEventArgs(100.0, _currentSimToken));

        ulong ComputeAvailable(int c)
        {
            ulong avail = ~(cols | diag1 | diag2) & mask;
            int maxRow;
            if (c == 0)
                maxRow = restrictFirstCol ? maxRow0 : N;
            else if (enhancedSymmetry && restrictFirstCol && c == 1)
            {
                int firstRow = queenRows[0];
                maxRow = ((N & 1) == 1 && firstRow == N / 2) ? N / 2 : N;
            }
            else
                maxRow = N;

            if (maxRow < N)
                avail &= (1UL << maxRow) - 1UL;
            return avail;
        }

        void Restore(int c, out ulong rem)
        {
            rem = stackRemaining[c];
            cols = stackCols[c];
            diag1 = stackD1[c];
            diag2 = stackD2[c];
        }
    }
    #endregion

    #region Fields / Helpers

    private bool ShouldAddSolution()
    {
        if (!_capEnabled) return true;
        int cap = SimulationSettings.MaxNoOfSolutionsInOutput;
        if (cap <= 0) return true;
        return _solutions.Count < cap;
    }

    private bool ShouldStopCollecting() =>
        _capEnabled && ShouldAddSolution() == false;

    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private ulong _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _capEnabled = true;
    private bool _disposed;
    private readonly int _maxSolutionsInOutput = maxSolutionsInOutput;
    #endregion
}
