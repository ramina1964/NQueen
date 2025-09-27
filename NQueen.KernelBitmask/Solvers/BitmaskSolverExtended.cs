namespace NQueen.KernelBitmask.Solvers;

public class BitmaskSolverExtended : ISolverPruning, IDisposable
{
    #region Ctors

    public BitmaskSolverExtended(ISolutionFormatter solutionFormatter, bool disableCap)
        : this(solutionFormatter, SimulationSettings.MaxNoOfSolutionsInOutput) => _disableCap = disableCap;

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

    #endregion Ctors

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

    #endregion IDisposable

    public BitmaskSolverExtended(ISolutionFormatter solutionFormatter, int maxSolutionsInOutput = SimulationSettings.MaxNoOfSolutionsInOutput)
    {
        _solutionFormatter = solutionFormatter;
        _maxSolutionsInOutput = maxSolutionsInOutput;
    }

    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    public int DelayInMillisec { get; set; }

    public int ProgressValue { get; set; }

    public int BoardSize { get; private set; }

    public SolutionMode SolutionMode { get; private set; }

    public DisplayMode DisplayMode { get; private set; }

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public bool IsSolverCanceled { get; set; }

    public bool EnableParallelization { get; set; } = true; // new flag

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
    {
        return Task.Run(() =>
        {
            BoardSize = simContext.BoardSize;
            SolutionMode = simContext.SolutionMode;
            DisplayMode = simContext.DisplayMode;
            EnableParallelization = simContext.EnableParallelization; // store flag
            return Solve();
        });
    }

    public SimulationResults Solve()
    {
        if (BoardSize <= 0) throw new InvalidOperationException("BoardSize must be set (>0) before solving.");
        _solutions.Clear();
        _solutionCount = 0;
        var sw = Stopwatch.StartNew();

        bool canParallel = EnableParallelization && BoardSize >= 10 && SolutionMode != SolutionMode.Single;
        switch (SolutionMode)
        {
            case SolutionMode.All:
                if (canParallel) SolveAllParallel(); else BitmaskIterative(rows => { _solutionCount++; _solutions.Add((int[])rows.Clone()); return false; }, restrictFirstCol:false, enhancedSymmetry:false);
                break;
            case SolutionMode.Unique:
                if (canParallel) SolveUniqueParallel();
                else
                {
                    var uniqueSet = new HashSet<int[]>(new IntArrayComparer());
                    var scratchBuf = new int[BoardSize];
                    BitmaskIterative(rows => {
                        var copy = (int[])rows.Clone();
                        if (SymmetryHelper.AddIfUnique(copy, uniqueSet, scratchBuf))
                        {
                            _solutionCount++;
                            _solutions.Add(copy);
                        }
                        return false;
                    }, restrictFirstCol:true, enhancedSymmetry:true);
                }
                break;
            case SolutionMode.Single:
                BitmaskIterative(rows => { _solutionCount++; if (_solutions.Count == 0) _solutions.Add((int[])rows.Clone()); return true; }, restrictFirstCol:false, enhancedSymmetry:false);
                break;
            default:
                throw new NotImplementedException("Unsupported SolutionMode value.");
        }
        sw.Stop();
        IEnumerable<int[]> outputSolutions = _solutions;
        // Only cap output for UI display (DisplayMode.Visualize)
        if (DisplayMode == DisplayMode.Visualize && BoardSize < 10 && SimulationSettings.MaxNoOfSolutionsInOutput > 0 && _solutions.Count > SimulationSettings.MaxNoOfSolutionsInOutput)
        {
            outputSolutions = _solutions.Take(SimulationSettings.MaxNoOfSolutionsInOutput);
        }
        var resultSolutions = outputSolutions.Select((sol, idx) => new Solution(sol, _solutionFormatter, idx + 1)).ToList();
        return new SimulationResults(resultSolutions, _solutionCount, Math.Round(sw.Elapsed.TotalSeconds, 1));
    }

    // --- Parallel variants (shallow top-level splitting). For large N, split first column placements among tasks.
    private void SolveAllParallel() => ParallelRootSplit(onSolution: rows => { _solutionCount++; if (ShouldAddSolution()) { lock (_solutions) { _solutions.Add(rows); if (EnableEvents) SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows))); } } return false; }, restrictFirstCol:false, enhancedSymmetry:false);
    private void SolveSingleParallel() => ParallelRootSplit(onSolution: rows => { _solutionCount++; if (_solutions.Count == 0) { lock (_solutions) { if (_solutions.Count == 0) { _solutions.Add(rows); if (EnableEvents) SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows))); } } return true; } return true; }, restrictFirstCol:false, enhancedSymmetry:false);
    private void SolveUniqueParallel()
    {
        int N = BoardSize;
        int maxRow0 = (N + 1) / 2;
        var tasks = new List<Task<HashSet<int[]>>>();
        for (int firstRow = 0; firstRow < maxRow0; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localUnique = new HashSet<int[]>(new IntArrayComparer());
                var scratchBuf = new int[N];
                var localRows = new int[N]; Array.Fill(localRows, -1); localRows[0] = fr;
                uint cols = 1u << fr; uint diag1 = (1u << fr) << 1; uint diag2 = (1u << fr) >> 1;
                var stack = new Stack<(int col, uint cols, uint d1, uint d2, int row)>();
                int col = 1; int row = 0; uint mask = N==32?0xFFFFFFFFu:(uint)((1u<<N)-1);
                while (true)
                {
                    if (col==N)
                    {
                        if (ValidationHelper.AreAllPositionsValid(localRows))
                        {
                            var copy = (int[])localRows.Clone();
                            if (SymmetryHelper.AddIfUnique(copy, localUnique, scratchBuf))
                            {
                                // Only add if unique in local set
                            }
                        }
                        if (stack.Count==0) break; (col, cols, diag1, diag2, _) = stack.Pop(); row = localRows[col]+1; continue;
                    }
                    int maxRow = col==0?maxRow0:N;
                    if (col==1)
                    {
                        int first = localRows[0];
                        maxRow = ((N & 1)==1 && first==N/2)? N/2 : N;
                    }
                    uint available = ~(cols | diag1 | diag2) & mask;
                    uint bit = 1u << row;
                    while (row < maxRow && (available & bit)==0){ row++; bit <<=1; }
                    if (row>=maxRow || (available & (1u<<row))==0)
                    { if (stack.Count==0) break; (col, cols, diag1, diag2, row)=stack.Pop(); row = localRows[col]+1; continue; }
                    localRows[col]=row; stack.Push((col, cols, diag1, diag2, row)); cols |= (1u<<row); diag1=(diag1 | (1u<<row))<<1; diag2=(diag2 | (1u<<row))>>1; col++; row=0;
                }
                return localUnique;
            }));
        }
        Task.WaitAll(tasks.ToArray());
        var globalUnique = new HashSet<int[]>(new IntArrayComparer());
        foreach (var t in tasks)
        {
            foreach (var sol in t.Result)
            {
                if (SymmetryHelper.AddIfUnique(sol, globalUnique, new int[N]))
                {
                    _solutionCount++;
                    if (ShouldAddSolution())
                    {
                        _solutions.Add((int[])sol.Clone());
                        if (EnableEvents) SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(sol)));
                    }
                }
            }
        }
        ProgressValueChanged?.Invoke(this, new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void ParallelRootSplit(Func<int[], bool> onSolution, bool restrictFirstCol, bool enhancedSymmetry, bool unique=false)
    {
        int N = BoardSize;
        int maxRow0 = restrictFirstCol ? (N + 1)/2 : N;
        var tasks = new List<Task>();
        var globalUnique = unique ? new HashSet<string>() : null; // simple string key uniqueness to avoid heavy transforms across tasks
        for (int firstRow = 0; firstRow < maxRow0; firstRow++)
        {
            int fr = firstRow;
            tasks.Add(Task.Run(() =>
            {
                var localRows = new int[N]; Array.Fill(localRows, -1); localRows[0] = fr;
                uint cols = 1u << fr; uint diag1 = (1u << fr) << 1; uint diag2 = (1u << fr) >> 1;
                var stack = new Stack<(int col, uint cols, uint d1, uint d2, int row)>();
                int col = 1; int row = 0; uint mask = N==32?0xFFFFFFFFu:(uint)((1u<<N)-1);
                var scratchUnique = unique ? new HashSet<int[]>(new IntArrayComparer()) : null;
                var scratchBuf = unique ? new int[N] : null;
                while (true)
                {
                    if (col==N)
                    {
                        if (ValidationHelper.AreAllPositionsValid(localRows))
                        {
                            var copy = (int[])localRows.Clone();
                            if (unique)
                            {
                                if (SymmetryHelper.AddIfUnique(copy, scratchUnique!, scratchBuf!))
                                {
                                    var key = string.Join(',', copy);
                                    lock (globalUnique!) { if (globalUnique.Add(key)) onSolution(copy); }
                                }
                            }
                            else
                            {
                                onSolution(copy);
                            }
                        }
                        if (stack.Count==0) break; (col, cols, diag1, diag2, _) = stack.Pop(); row = localRows[col]+1; continue;
                    }
                    int maxRow = col==0?maxRow0:N;
                    if (enhancedSymmetry && restrictFirstCol && col==1)
                    {
                        int first = localRows[0];
                        maxRow = ((N & 1)==1 && first==N/2)? N/2 : N;
                    }
                    uint available = ~(cols | diag1 | diag2) & mask;
                    uint bit = 1u << row;
                    while (row < maxRow && (available & bit)==0){ row++; bit <<=1; }
                    if (row>=maxRow || (available & (1u<<row))==0)
                    { if (stack.Count==0) break; (col, cols, diag1, diag2, row)=stack.Pop(); row = localRows[col]+1; continue; }
                    localRows[col]=row; stack.Push((col, cols, diag1, diag2, row)); cols |= (1u<<row); diag1=(diag1 | (1u<<row))<<1; diag2=(diag2 | (1u<<row))>>1; col++; row=0;
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        ProgressValueChanged?.Invoke(this, new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void BitmaskIterative(Func<int[], bool> onSolution,
        bool restrictFirstCol = false, bool enhancedSymmetry = false)
    {
        var N = BoardSize;
        var queenRows = new int[N];
        Array.Fill(queenRows, -1);
        var col = 0;
        var row = 0;

        if (N > 31)
            throw new NotSupportedException("Bitmask solver supports board sizes up to 31.");
        uint mask = N == 32 ? 0xFFFFFFFFu : (uint)((1u << N) - 1);
        uint cols = 0;
        uint diag1 = 0;
        uint diag2 = 0;

        Stack<(int col, uint cols, uint diag1, uint diag2, int row)> stack = new();
        var maxRow0 = restrictFirstCol ? (N + 1) / 2 : N;

        // --- Progress based on root-level placements ---
        int rootPlacements = 0;
        int rootTotal = restrictFirstCol ? maxRow0 : N;
        ProgressValueChanged?.Invoke(this,
            new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(0.0, _currentSimToken));

        var visualize = DisplayMode == DisplayMode.Visualize;
        var delay = visualize && DelayInMillisec > 0 ? DelayInMillisec : 0;
        int queenPlacedSampleRate = N >= 12 ? 1000 : 1;
        int queenPlacedCounter = 0;
        int lastDepth = -1;

        while (true)
        {
            if (IsSolverCanceled)
                break;

            if (col == N)
            {
                if (ValidationHelper.AreAllPositionsValid(queenRows))
                {
                    if (onSolution(queenRows))
                        break;
                }

                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, _) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }

            int maxRow;
            if (col == 0)
                maxRow = restrictFirstCol ? maxRow0 : N;
            else if (enhancedSymmetry && restrictFirstCol && col == 1)
            {
                int firstRow = queenRows[0];
                if ((N & 1) == 1 && firstRow == N / 2)
                    maxRow = N / 2;
                else
                    maxRow = N;
            }
            else
                maxRow = N;

            uint available = ~(cols | diag1 | diag2) & mask;
            uint bit = 1u << row;

            while (row < maxRow && (available & bit) == 0) { row++; bit <<= 1; }
            if (row >= maxRow || (available & (1u << row)) == 0)
            {
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, row) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }

            queenRows[col] = row;

            // Progress: count root-level placements
            if (col == 0)
            {
                rootPlacements++;
                var pct = (double)rootPlacements / rootTotal * 100.0;
                ProgressValueChanged?.Invoke(this,
                    new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(pct, _currentSimToken));
            }

            if (visualize)
            {
                queenPlacedCounter++;
                if (queenPlacedCounter % queenPlacedSampleRate == 0 || col > lastDepth)
                {
                    QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(new Memory<int>(queenRows)));
                    lastDepth = col;
                }
                if (delay > 0)
                    Thread.Sleep(delay);
            }

            stack.Push((col, cols, diag1, diag2, row));
            cols |= (1u << row);
            diag1 = (diag1 | (1u << row)) << 1;
            diag2 = (diag2 | (1u << row)) >> 1;
            col++;
            row = 0;
        }
        // Emit final 100% progress event
        ProgressValueChanged?.Invoke(this,
            new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private readonly ISolutionFormatter _solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private int _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _disableCap = false;
    private bool _disposed;
    private readonly int _maxSolutionsInOutput;

    public bool EnableEvents { get; set; } = true;

    private bool ShouldAddSolution() => _maxSolutionsInOutput <= 0 || _solutions.Count < _maxSolutionsInOutput;
}
