namespace NQueen.KernelBitmask.Solvers;

/// <summary>
/// Minimal iterative bitmask N-Queens solver (reference / baseline).
/// Provides an allocation-light stack-based enumeration for all solutions (up to N=32)
/// without symmetry pruning. An optional symmetry-pruned variant (<see cref="SolveAllSymmetryPruned"/>)
/// is included for experimentation / benchmarking.
/// </summary>
/// <remarks>
/// BIT REPRESENTATION:
/// <list type="bullet">
/// <item><description><c>cols</c>  – occupied rows.</description></item>
/// <item><description><c>diag1</c> – main diagonals (shift left per column advance).</description></item>
/// <item><description><c>diag2</c> – anti-diagonals (shift right per column advance).</description></item>
/// </list>
/// AVAILABLE ROWS = <c>~(cols | diag1 | diag2) & mask</c> where <c>mask = (1 &lt;&lt; N) - 1</c>.
/// </remarks>
public class BitmaskSolver(int boardSize)
{
    /// <summary>
    /// Enumerates all solutions (no symmetry pruning). Optionally collects first-class copies.
    /// </summary>
    /// <param name="collectSolutions">If true, solutions are cloned and stored in <see cref="Solutions"/>.</param>
    /// <returns>Total number of solutions discovered.</returns>
    public int SolveAll(bool collectSolutions = false)
    {
        SolutionCount = 0;
        Solutions.Clear();
        if (BoardSize < 1 || BoardSize > 32)
            throw new ArgumentOutOfRangeException(nameof(BoardSize),
                "Supported N is 1..32");

        int n = BoardSize;
        var queenRows = new int[n];
        Array.Fill(queenRows, -1);
        int row = 0, col = 0;
        uint mask = (uint)((1 << n) - 1);
        uint cols = 0;
        uint diag1 = 0;
        uint diag2 = 0;
        Stack<(int col, uint cols, uint diag1, uint diag2)> stack = new();

        while (true)
        {
            uint available = ~(cols | diag1 | diag2) & mask;
            if (col == n)
            {
                SolutionCount++;
                if (collectSolutions)
                    Solutions.Add((int[])queenRows.Clone());
                // Backtrack
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }
            if (available == 0)
            {
                // Backtrack
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }
            // Find next available row >= current row
            uint bit = 1u << row;
            while ((available & bit) == 0 && row < n) bit <<= 1;
            if (row >= n || (available & bit) == 0)
            {
                // No more rows in this column, backtrack
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }
            // Place queen
            queenRows[col] = row;
            stack.Push((col, cols, diag1, diag2));
            cols |= bit;
            diag1 = (diag1 | bit) << 1;
            diag2 = (diag2 | bit) >> 1;
            col++;
            row = 0;
        }
        return SolutionCount;
    }

    /// <summary>
    /// Symmetry-pruned enumeration variant (horizontal + second-column center pruning). Provided for tests / benchmarking.
    /// Not used implicitly by existing callers.
    /// </summary>
    public int SolveAllSymmetryPruned(bool collectSolutions = false)
    {
        SolutionCount = 0;
        Solutions.Clear();
        if (BoardSize < 1 || BoardSize > 32)
            throw new ArgumentOutOfRangeException(nameof(BoardSize),
                "Supported N is 1..32");

        int n = BoardSize;
        var queenRows = new int[n];
        Array.Fill(queenRows, -1);
        int row = 0, col = 0;
        uint mask = (uint)((1 << n) - 1);
        uint cols = 0, diag1 = 0, diag2 = 0;
        Stack<(int col, uint cols, uint diag1, uint diag2, int row)> stack = new();
        int maxRow0 = (n + 1) / 2; // horizontal symmetry for col 0

        while (true)
        {
            if (col == n)
            {
                SolutionCount++;
                if (collectSolutions)
                    Solutions.Add((int[])queenRows.Clone());
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, _) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }

            int colRowLimit;
            if (col == 0)
            {
                colRowLimit = maxRow0;
            }
            else if (col == 1)
            {
                int firstRow = queenRows[0];
                if ((n & 1) == 1 && firstRow == n / 2)
                {
                    // first queen centered -> only explore second column rows strictly above center
                    colRowLimit = n / 2;
                }
                else
                {
                    colRowLimit = n;
                }
            }
            else
            {
                colRowLimit = n;
            }

            uint available = ~(cols | diag1 | diag2) & mask;
            if (available == 0)
            {
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, row) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }

            uint bit = 1u << row;
            while (row < colRowLimit && (available & bit) == 0) { row++; bit <<= 1; }
            if (row >= colRowLimit || (available & bit) == 0)
            {
                if (stack.Count == 0) break;
                (col, cols, diag1, diag2, row) = stack.Pop();
                row = queenRows[col] + 1;
                continue;
            }

            queenRows[col] = row;
            stack.Push((col, cols, diag1, diag2, row));
            cols |= bit;
            diag1 = (diag1 | bit) << 1;
            diag2 = (diag2 | bit) >> 1;
            col++;
            row = 0;
        }

        return SolutionCount;
    }

    public int BoardSize { get; } = boardSize;

    /// <summary>Total number of solutions from the last execution.</summary>
    public int SolutionCount { get; private set; }

    /// <summary>Collected solutions (if <see cref="SolveAll"/> or <see cref="SolveAllSymmetryPruned"/> invoked with collection enabled).</summary>
    public List<int[]> Solutions { get; } = new();
}
