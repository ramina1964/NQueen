// BitmaskSolverEngineFull: High-performance iterative N-Queens solver using bitmasks (bitboards)
// Supports All, Unique, and Single solution modes, and emits events for UI/visualization.
// Bitmask/bitboard technique allows O(1) pruning for queen placement.
// Symmetry pruning is handled for Unique mode using SymmetryHelper.

namespace NQueen.KernelBitmask.Solvers;

/// <summary>
/// High-performance bitmask (bitboard) based N-Queens solver providing:
/// <list type="bullet">
/// <item><description>Three solution modes: <see cref="SolutionMode.All"/>, <see cref="SolutionMode.Unique"/>, and <see cref="SolutionMode.Single"/>.</description></item>
/// <item><description>Iterative (stack-based) backtracking avoiding recursion and excess allocations.</description></item>
/// <item><description>Bitmask constraint propagation: occupied columns / main diagonals / anti-diagonals maintained in <c>O(1)</c>.</description></item>
/// <item><description>Event hooks for UI / progress visualization (queen placement, solution found, progress updates).</description></item>
/// <item><description>Optional output capping via <see cref="SimulationSettings.MaxNoOfSolutionsInOutput"/>.</description></item>
/// <item><description>Enhanced symmetry pruning for <see cref="SolutionMode.Unique"/> (horizontal first-column reduction + second-column pruning when the first queen is centered for odd boards).</description></item>
/// </list>
/// </summary>
/// <remarks>
/// PRUNING (Unique Mode):
/// <para>1. Horizontal First-Column Pruning: For uniqueness we only explore rows <c>[0 .. (N-1)/2]</c> (inclusive of middle when N odd) for column 0. Mirrors are reconstructed implicitly by the symmetry uniqueness test.</para>
/// <para>2. Second-Column Center Reduction: When N is odd and the first queen is on the middle row, only rows strictly above center are explored for column 1. This eliminates horizontally mirrored duplicates earlier.</para>
/// <para>3. Fast Symmetry Rejection: Each solution candidate is passed to <see cref="SymmetryHelper.AddIfUnique(int[], System.Collections.Generic.HashSet{int[]}, int[])"/> which inlines eight-way symmetry (reflections + rotations) using a single reusable scratch buffer and early exits on first match.</para>
/// The solver does not precompute transformation maps and allocates minimally.
/// </remarks>
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

    /// <summary>Raised every time a queen position is committed for a column (may include provisional / partial placements).</summary>
    public event EventHandler<QueenPlacedEventArgs>? QueenPlaced;
    /// <summary>Raised when a full valid solution has been found (subject to mode and symmetry filtering).</summary>
    public event EventHandler<SolutionFoundEventArgs>? SolutionFound;
    /// <summary>Raised periodically (approximate heuristic based on explored nodes) to report progress.</summary>
    public event EventHandler<ProgressUpdateEventArgs>? ProgressValueChanged;

    /// <summary>Optional visualization delay (ms) when UI mode is active.</summary>
    public int DelayInMillisec { get; set; }

    /// <summary>Arbitrary progress scalar consumed by UI.</summary>
    public int ProgressValue { get; set; }

    /// <summary>Board size (N). Must be &gt; 0 before solving.</summary>
    public int BoardSize { get; private set; }

    /// <summary>Active solution enumeration mode.</summary>
    public SolutionMode SolutionMode { get; private set; }

    /// <summary>Display semantics for visualization (not used in this backend core, but propagated).</summary>
    public DisplayMode DisplayMode { get; private set; }

    /// <summary>Assigns a UI simulation token for progress correlation.</summary>
    public void SetSimulationToken(Guid token) => _currentSimToken = token;

    /// <summary>External cancellation signal (checked cooperatively by caller if expanded in future).</summary>
    public bool IsSolverCanceled { get; set; }

    /// <summary>
    /// Initializes the solver from a <see cref="SimulationContext"/> and executes synchronously.
    /// </summary>
    public Task<SimulationResults> GetSimResultsAsync(SimulationContext simContext)
    {
        BoardSize = simContext.BoardSize;
        SolutionMode = simContext.SolutionMode;
        DisplayMode = simContext.DisplayMode;
        var result = Solve();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Executes solve based on current properties. Dispatches to the appropriate strategy per <see cref="SolutionMode"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">If <see cref="BoardSize"/> is not positive.</exception>
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

    /// <summary>
    /// Enumerates every solution (no symmetry pruning) until exhaustion.
    /// </summary>
    private void SolveAll()
    {
        BitmaskIterative(solution =>
        {
            _solutionCount++;
            if (_disableCap || _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput)
                _solutions.Add((int[])solution.Clone());
            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(solution)));
            return false; // continue enumeration
        });
    }

    /// <summary>
    /// Finds the first solution only. Enumeration stops immediately after first full placement.
    /// </summary>
    private void SolveSingle()
    {
        // For Single we use the full column-0 range to preserve ordering expectation.
        BitmaskIterative(solution =>
        {
            _solutionCount++;
            if (_solutions.Count == 0)
                _solutions.Add((int[])solution.Clone());

            SolutionFound?.Invoke(this, new SolutionFoundEventArgs(
                new Memory<int>(solution)));

            return true; // stop enumeration
        });
    }

    /// <summary>
    /// Enumerates unique (non-symmetric) solutions using half-board + second-column pruning and
    /// fast symmetry rejection via <see cref="SymmetryHelper.AddIfUnique(int[], System.Collections.Generic.HashSet{int[]}, int[])"/>.
    /// </summary>
    private void SolveUnique()
    {
        var uniqueSolutions = new HashSet<int[]>(new IntArrayComparer());
        var scratch = new int[BoardSize]; // reusable buffer for symmetry checks
        BitmaskIterative(solution =>
        {
            if (SymmetryHelper.AddIfUnique(solution, uniqueSolutions, scratch))
            {
                _solutionCount++;
                var canStoreSolution = _disableCap ||
                    _solutions.Count < SimulationSettings.MaxNoOfSolutionsInOutput;

                if (canStoreSolution)
                    _solutions.Add((int[])solution.Clone());

                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(
                    new Memory<int>(solution)));
            }
            return false; // continue enumeration (all unique forms)
        }, restrictFirstCol: true, enhancedSymmetry: true);
    }

    /// <summary>
    /// Core iterative backtracking engine.
    /// </summary>
    /// <param name="onSolution">Callback invoked with a mutable working array (clone if storing). Return <c>true</c> to terminate early.</param>
    /// <param name="restrictFirstCol">If <c>true</c>, restricts column 0 placements to half the board (horizontal symmetry).</param>
    /// <param name="enhancedSymmetry">If <c>true</c> and <paramref name="restrictFirstCol"/> is also true, enables second-column pruning for centered first queen (odd N).</param>
    /// <remarks>
    /// Maintains three bitmasks:
    /// <list type="bullet">
    /// <item><description><c>cols</c>: occupied rows (one bit per row)</description></item>
    /// <item><description><c>diag1</c>: main diagonal (shift-left each column advance)</description></item>
    /// <item><description><c>diag2</c>: anti-diagonal (shift-right each column advance)</description></item>
    /// </list>
    /// Candidate rows for a column are computed by negating the OR of these and ANDing with a full-mask.
    /// </remarks>
    private void BitmaskIterative(Func<int[], bool> onSolution, bool restrictFirstCol = false, bool enhancedSymmetry = false)
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
        // Horizontal symmetry pruning: only explore half (and middle row when odd) for the first column.
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

            // Determine max row boundary for the current column when enhanced symmetry pruning is active.
            int maxRow;
            if (col == 0)
            {
                maxRow = restrictFirstCol ? maxRow0 : N;
            }
            else if (enhancedSymmetry && restrictFirstCol && col == 1)
            {
                // Additional pruning for the second column when the first queen is in the center row (odd N).
                int firstRow = queenRows[0];
                if ((N & 1) == 1 && firstRow == N / 2)
                {
                    // Allow only rows strictly above the center.
                    maxRow = N / 2; // exclusive upper bound already handled by loop condition.
                }
                else
                {
                    maxRow = N;
                }
            }
            else
            {
                maxRow = N;
            }

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

    /// <summary>Disposes managed resources (event handlers & cached solution list).</summary>
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
