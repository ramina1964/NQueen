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

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
    {
        return Task.Run(() =>
        {
            BoardSize = simContext.BoardSize;
            SolutionMode = simContext.SolutionMode;
            DisplayMode = simContext.DisplayMode;
            return Solve();
        });
    }

    public SimulationResults Solve()
    {
        if (BoardSize <= 0) throw new InvalidOperationException("BoardSize must be set (>0) before solving.");
        _solutions.Clear();
        _solutionCount = 0;
        var sw = Stopwatch.StartNew();

        switch (SolutionMode)
        {
            case SolutionMode.All:
                SolveAll();
                break;

            case SolutionMode.Unique:
                SolveUnique();
                break;

            case SolutionMode.Single:
                SolveSingle();
                break;

            default:
                throw new NotImplementedException("Unsupported SolutionMode value.");
        }

        sw.Stop();

        var resultSolutions = _solutions
            .Select((sol, idx) => new Solution(sol, _solutionFormatter, idx + 1));

        return new SimulationResults(
            resultSolutions,
            _solutionCount,
            Math.Round(sw.Elapsed.TotalSeconds, 1));
    }

    private bool ShouldAddSolution() =>
        _maxSolutionsInOutput <= 0 || _solutions.Count < _maxSolutionsInOutput;

    private void SolveAll()
    {
        int progressBatchStep = BoardSize switch
        {
            <= 8 => 1,
            <= 12 => 10,
            <= 16 => 100,
            _ => 1000
        };
        int progressCurrent = 0;
        ProgressValueChanged?.Invoke(this,
            new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(0.0, _currentSimToken));

        BitmaskIterative(solution =>
        {
            _solutionCount++;
            progressCurrent++;
            if (progressCurrent % progressBatchStep == 0)
            {
                // Emit progress as a fraction of solutions found (never 100 until end)
                var pct = Math.Min((double)progressCurrent / (progressCurrent + progressBatchStep) * 100.0, 99.0);
                ProgressValueChanged?.Invoke(this,
                    new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(pct, _currentSimToken));
            }
            if (ShouldAddSolution())
            {
                _solutions.Add((int[])solution.Clone());
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            }
            return false;
        });
        // Emit final 100% progress event
        ProgressValueChanged?.Invoke(this,
            new NQueen.Domain.EventArgsPruning.ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void SolveSingle()
    {
        BitmaskIterative(solution =>
        {
            _solutionCount++;
            if (_solutions.Count == 0)
            {
                _solutions.Add((int[])solution.Clone());
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            }
            // After first solution, do not emit SolutionFound
            return true;
        });
    }

    private void SolveUnique()
    {
        var uniqueSolutions = new HashSet<int[]>(new IntArrayComparer());
        var scratch = new int[BoardSize];
        BitmaskIterative(solution =>
        {
            if (SymmetryHelper.AddIfUnique(solution, uniqueSolutions, scratch))
            {
                _solutionCount++;
                if (ShouldAddSolution())
                {
                    _solutions.Add((int[])solution.Clone());
                    SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
                }
                // After cap reached, do not emit SolutionFound
            }
            return false;
        }, restrictFirstCol: true, enhancedSymmetry: true);
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
}
