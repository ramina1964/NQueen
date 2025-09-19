// BitmaskSolverEngineFull: High-performance iterative N-Queens solver using bitmasks (bitboards)
// Supports All, Unique, and Single solution modes, and emits events for UI/visualization.
// Bitmask/bitboard technique allows O(1) pruning for queen placement.
// Symmetry pruning is handled for Unique mode using SymmetryHelper.

namespace NQueen.KernelBitmask.Solvers;

public class BitmaskSolverEngineFull(
    int boardSize,
    SolutionMode solutionMode,
    DisplayMode displayMode,
    ISolutionFormatter solutionFormatter)
{
    // Event: Raised when a queen is placed (for visualization)
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    
    // Event: Raised when a solution is found
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    
    // Event: Raised when progress is updated (not used in this core, but available)
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    // Delay for visualization (ms)
    public int DelayInMillisec { get; set; }

    public int ProgressValue { get; set; }

    public int BoardSize { get; } = boardSize;

    public SolutionMode SolutionMode { get; } = solutionMode;

    public DisplayMode DisplayMode { get; } = displayMode;

    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    // Main entry: Solves the N-Queens problem for the configured mode
    public SimulationResults Solve()
    {
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

        // Keep only a display slice, but preserve total via _solutionCount
        var resultSolutions = _solutions
            .Take(SimulationSettings.MaxNoOfSolutionsInOutput)
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
            _solutions.Add((int[])solution.Clone());
            _solutionCount++;
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            return false; // keep searching
        });
    }

    // Solve for a single solution (stop after first)
    private void SolveSingle()
    {
        BitmaskIterative((solution) =>
        {
            _solutions.Add((int[])solution.Clone());
            _solutionCount++;
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
            // Use SymmetryHelper to check if any symmetrical transformation is already seen
            if (!SymmetryHelper.GetSymmetricalSolutions(solution).Any(seen.Contains))
            {
                seen.Add((int[])solution.Clone());
                _solutions.Add((int[])solution.Clone());
                _solutionCount++;
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            }
            return false;
        }, restrictFirstCol: true);
    }

    // Core iterative bitmask/bitboard solver
    // Uses bitmasks to track occupied columns, main diagonals, and anti-diagonals
    // Each bit in cols/diag1/diag2 represents a row (for columns), or a diagonal
    // Stack is used to avoid recursion (faster, avoids stack overflow)
    // restrictFirstCol: for symmetry pruning, restricts first queen to left half
    private void BitmaskIterative(Func<int[], bool> onSolution, bool restrictFirstCol = false)
    {
        int n = BoardSize;
        int[] queenRows = new int[n]; // queenRows[col] = row index of queen in column col
        Array.Fill(queenRows, -1);
        int col = 0, row = 0;
        uint mask = (uint)((1 << n) - 1); // n bits set to 1
        uint cols = 0, diag1 = 0, diag2 = 0; // bitmasks for columns, main diag, anti-diag
        Stack<(int col, uint cols, uint diag1, uint diag2, int row)> stack = new();
        int maxRow0 = restrictFirstCol ? (n + 1) / 2 : n; // symmetry: only left half for col 0
        int progressStep = Math.Max(1, n * n / 100);
        int progressCounter = 0;
        while (true)
        {
            if (col == n)
            {
                // All queens placed, found a solution
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
            // Find next available row >= current row
            uint bit = 1u << row;
            while (row < maxRow && (available & bit) == 0) { row++; bit <<= 1; }
            if (row >= maxRow || (available & (1u << row)) == 0)
            {
                // No more rows in this column, backtrack
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, row) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }
            // Place queen at (col, row)
            queenRows[col] = row;
            QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(new Memory<int>(queenRows)));

            // Progress event usage
            progressCounter++;
            if (progressCounter % progressStep == 0)
            {
                ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(
                    (double)progressCounter / (n * n), _currentSimToken));
            }

            stack.Push((col, cols, diag1, diag2, row));
            cols |= (1u << row); // mark row as occupied
            diag1 = (diag1 | (1u << row)) << 1; // mark main diagonal
            diag2 = (diag2 | (1u << row)) >> 1; // mark anti-diagonal
            col++;
            row = 0;
        }
    }

    private readonly ISolutionFormatter _solutionFormatter = solutionFormatter;
    private readonly List<int[]> _solutions = [];
    private int _solutionCount;
    private Guid _currentSimToken = Guid.Empty;
}
