// BitmaskSolverEngineFull: High-performance iterative N-Queens solver using bitmasks (bitboards)
// Supports All, Unique, and Single solution modes, and emits events for UI/visualization.
// Bitmask/bitboard technique allows O(1) pruning for queen placement.
// Symmetry pruning is handled for Unique mode using SymmetryHelper.

namespace NQueen.KernelBitmask.Solvers;

public class BitmaskSolverExtended : ISolverBackEndPruning, ISolverBackEnd
{
    // Constructors
    public BitmaskSolverExtended(ISolutionFormatter solutionFormatter)
    {
        _solutionFormatter = solutionFormatter;
    }

    // Optional constructor to control result capping (used by tests to disable cap)
    public BitmaskSolverExtended(ISolutionFormatter solutionFormatter, bool disableCap)
        : this(solutionFormatter) => _disableCap = disableCap;

    // Backward compatible constructor (sets properties immediately, keeps old calling code working)
    public BitmaskSolverExtended(int boardSize, SolutionMode solutionMode, DisplayMode displayMode, ISolutionFormatter solutionFormatter)
        : this(solutionFormatter)
    {
        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;
    }

    // Event: Raised when a queen is placed (for visualization)
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    
    // Event: Raised when a solution is found
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    
    // Event: Raised when progress is updated (not used in this core, but available)
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    // Delay for visualization (ms)
    public int DelayInMillisec { get; set; }

    public int ProgressValue { get; set; }

    public int BoardSize { get; private set; }

    public SolutionMode SolutionMode { get; private set; }

    public DisplayMode DisplayMode { get; private set; }

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    // ISolverBackEndPruning & ISolverBackEnd shared cancellation flag
    public bool IsSolverCanceled { get; set; }

    // New wrapper for legacy ISolverBackEnd signature
    public Task<SimulationResults> GetSimResultsAsync(int boardSize, SolutionMode solutionMode, DisplayMode displayMode = DisplayMode.Hide)
    {
        BoardSize = boardSize;
        SolutionMode = solutionMode;
        DisplayMode = displayMode;
        var result = Solve();
        return Task.FromResult(result);
    }

    // Context-based API
    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
    {
        BoardSize = simContext.BoardSize;
        SolutionMode = simContext.SolutionMode;
        DisplayMode = simContext.DisplayMode;
        var result = Solve();
        return Task.FromResult(result);
    }

    // Main entry: Solves the N-Queens problem for the configured mode
    public SimulationResults Solve()
    {
        if (BoardSize <= 0) throw new InvalidOperationException("BoardSize must be set (>0) before solving.");
        _solutions.Clear();
        _solutionCount = 0;
        var sw = Stopwatch.StartNew();

        if (SolutionMode == SolutionMode.All)
            SolveAll();
        else if (SolutionMode == SolutionMode.Unique)
            SolveUnique();
        else
            SolveSingle();

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

    // Solve for all solutions (no symmetry pruning)
    private void SolveAll()
    {
        BitmaskIterative((solution) =>
        {
            _solutionCount++;
            if (_disableCap || _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput)
                _solutions.Add((int[])solution.Clone());
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            return false;
        });
    }

    // Solve for a single solution (stop after first)
    private void SolveSingle()
    {
        BitmaskIterative((solution) =>
        {
            _solutionCount++;
            if (_solutions.Count == 0) // only keep first regardless of cap setting
                _solutions.Add((int[])solution.Clone());
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            return true; // stop after first
        });
    }

    // Solve for unique (non-symmetrical) solutions using symmetry pruning
    private void SolveUnique()
    {
        var seen = new HashSet<int[]>(new IntArrayComparer());
        BitmaskIterative((solution) =>
        {
            if (!SymmetryHelper.GetSymmetricalSolutions(solution).Any(seen.Contains))
            {
                seen.Add((int[])solution.Clone());
                _solutionCount++;
                if (_disableCap || _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput)
                    _solutions.Add((int[])solution.Clone());
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            }
            return false;
        }, restrictFirstCol: true);
    }

    // Core iterative bitmask/bitboard solver
    private void BitmaskIterative(Func<int[], bool> onSolution, bool restrictFirstCol = false)
    {
        int n = BoardSize;
        int[] queenRows = new int[n];
        Array.Fill(queenRows, -1);
        int col = 0, row = 0;
        uint mask = (uint)((1 << n) - 1);
        uint cols = 0, diag1 = 0, diag2 = 0;
        Stack<(int col, uint cols, uint diag1, uint diag2, int row)> stack = new();
        int maxRow0 = restrictFirstCol ? (n + 1) / 2 : n;
        int progressStep = Math.Max(1, n * n / 100);
        int progressCounter = 0;
        while (true)
        {
            if (col == n)
            {
                if (ValidationHelper.AreAllPositionsValid(queenRows))
                {
                    if (onSolution(queenRows))
                        break;
                }
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, row) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }
            int maxRow = (col == 0 && restrictFirstCol) ? maxRow0 : n;
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
                    (double)progressCounter / (n * n), _currentSimToken));
            }

            stack.Push((col, cols, diag1, diag2, row));
            cols |= (1u << row);
            diag1 = (diag1 | (1u << row)) << 1;
            diag2 = (diag2 | (1u << row)) >> 1;
            col++;
            row = 0;
        }
    }

    private readonly ISolutionFormatter _solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private int _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
    private readonly bool _disableCap = false; // default keeps application behavior unless test opts out
}
