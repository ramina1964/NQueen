// BitmaskSolverEngineFull: High-performance iterative N-Queens solver using bitmasks (bitboards)
// Supports All, Unique, and Single solution modes, and emits events for UI/visualization.
// Bitmask/bitboard technique allows O(1) pruning for queen placement.
// Symmetry pruning is handled for Unique mode using SymmetryHelper.

namespace NQueen.KernelBitmask.Solvers;

public class BitmaskSolverExtended(ISolutionFormatter solutionFormatter)
    : ISolverBackEndPruning, IDisposable
{
    // Optional constructor to control result capping (used by tests to disable cap)
    public BitmaskSolverExtended(ISolutionFormatter solutionFormatter, bool disableCap)
        : this(solutionFormatter) => _disableCap = disableCap;

    public BitmaskSolverExtended(int boardSize, SolutionMode solutionMode,
        DisplayMode displayMode, ISolutionFormatter solutionFormatter)
            : this(solutionFormatter)
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

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    public bool IsSolverCanceled { get; set; }

    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
    {
        BoardSize = simContext.BoardSize;
        SolutionMode = simContext.SolutionMode;
        DisplayMode = simContext.DisplayMode;
        var result = Solve();
        return Task.FromResult(result);
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
                throw new ArgumentOutOfRangeException(nameof(SolutionMode), SolutionMode, "Unsupported SolutionMode value.");
        }

        sw.Stop();

        IEnumerable<int[]> selectedSolutions = _disableCap
            ? _solutions
            : _solutions.Take(SimulationSettings.MaxNoOfSolutionsInOutput);

        var resultSolutions = selectedSolutions
            .Select((sol, idx) => new Solution(sol, _solutionFormatter, idx + 1));

        return new SimulationResults(
            resultSolutions,
            _solutionCount,
            Math.Round(sw.Elapsed.TotalSeconds, 1));
    }

    private void SolveAll()
    {
        BitmaskIterative(solution =>
        {
            _solutionCount++;
            if (_disableCap || _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput)
                _solutions.Add((int[])solution.Clone());
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            return false;
        });
    }

    private void SolveSingle()
    {
        BitmaskIterative(solution =>
        {
            _solutionCount++;
            if (_solutions.Count == 0)
                _solutions.Add((int[])solution.Clone());

            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(
                new Memory<int>(solution)));

            return true;
        });
    }

    private void SolveUnique()
    {
        var uniqueSolutions = new HashSet<int[]>(new IntArrayComparer());
        BitmaskIterative(solution =>
        {
            if (SymmetryHelper.GetSymmetricalSolutions(solution)
                .Any(uniqueSolutions.Contains) == false)
            {
                uniqueSolutions.Add((int[])solution.Clone());
                _solutionCount++;
                var canStoreSolution = _disableCap ||
                    _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput;

                if (canStoreSolution)
                    _solutions.Add((int[])solution.Clone());

                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(
                    new Memory<int>(solution)));
            }

            return false;
        }, restrictFirstCol: true);
    }

    private void BitmaskIterative(Func<int[], bool> onSolution, bool restrictFirstCol = false)
    {
        var N = BoardSize;
        var queenRows = new int[N];
        Array.Fill(queenRows, -1);
        var col = 0;
        var row = 0;

        uint mask = (uint)((1 << N) - 1);
        uint cols = 0;
        uint diag1 = 0;
        uint diag2 = 0;

        Stack<(int col, uint cols, uint diag1, uint diag2, int row)> stack = new();
        var maxRow0 = restrictFirstCol
            ? (N + 1) / 2
            : N;

        var progressStep = Math.Max(1, N * N / 100);
        var progressCounter = 0;

        while (true)
        {
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

            var maxRow = (col == 0 && restrictFirstCol) ? maxRow0 : N;
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
            QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(new Memory<int>(queenRows)));

            progressCounter++;
            if (progressCounter % progressStep == 0)
            {
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(
                    (double)progressCounter / (N * N), _currentSimToken));
            }

            stack.Push((col, cols, diag1, diag2, row));
            cols |= (1u << row);
            diag1 = (diag1 | (1u << row)) << 1;
            diag2 = (diag2 | (1u << row)) >> 1;
            col++;
            row = 0;
        }
    }

    // Disposal pattern (managed only)
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

    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private int _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _disableCap = false;
    private bool _disposed;
}
