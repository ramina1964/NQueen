namespace NQueen.KernelBitmask.Solvers;

public class BitmaskSolver(int boardSize)
{
    public ulong SolveAll(bool collectSolutions = false)
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

    public ulong SolveAllSymmetryPruned(bool collectSolutions = false)
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
    public ulong SolutionCount { get; private set; }

    /// <summary>Collected solutions (if <see cref="SolveAll"/> or <see cref="SolveAllSymmetryPruned"/> invoked with collection enabled).</summary>
    public List<int[]> Solutions { get; } = new();
}
