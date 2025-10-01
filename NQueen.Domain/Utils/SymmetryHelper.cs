namespace NQueen.Domain.Utils;

// Todo: Keep methods used in NQueen.KernelBitmask only in this class, remove all others.
/// <summary>
/// Symmetry utilities for the N-Queens problem.
/// Focuses on efficient uniqueness detection via transformation generation and
/// in-place scratch-buffer comparisons. Multiple API layers retained for backward
/// compatibility; <see cref="AddIfUnique"/> is the preferred high-performance path.
/// </summary>
public static partial class SymmetryHelper
{
    /// <summary>
    /// Determines maximum (exclusive) row index to explore for a given column under enhanced symmetry pruning.
    /// Used by advanced solvers to restrict search (horizontal + secondary pruning).
    /// </summary>
    /// <param name="boardSize">Board dimension (N).</param>
    /// <param name="column">Current column index.</param>
    /// <param name="queenRows">Working queen row placements.</param>
    /// <returns>Exclusive upper bound for row iteration.</returns>
    public static int MaxRowExclusiveForColumn(int boardSize, int column, int[] queenRows)
    {
        if (column == 0)
            return (boardSize + 1) / 2;
        if (column == 1)
        {
            int firstRow = queenRows[0];
            if ((boardSize & 1) == 1 && firstRow == boardSize / 2)
                return boardSize / 2; // strictly above center
        }
        return boardSize;
    }

    /// <summary>
    /// Adds a solution to a uniqueness set if its canonical form is not already present.
    /// Uses a single reusable scratch buffer to avoid per-check allocations.
    /// </summary>
    public static bool AddIfUnique(int[] solution, HashSet<int[]> uniqueSolutions, int[] scratch)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueSolutions);
        ArgumentNullException.ThrowIfNull(scratch);
        if (scratch.Length < solution.Length)
            throw new ArgumentException("Scratch buffer too small", nameof(scratch));

        // Compute canonical form
        var canonical = GetCanonicalForm(solution);
        if (uniqueSolutions.Contains(canonical))
            return false;
        uniqueSolutions.Add((int[])canonical.Clone());
        return true;
    }

    /// <summary>
    /// Checks if any symmetrical transformation already exists (Memory<int> direct version).</summary>
    public static bool IsSymmetrical(
        Memory<int> solution,
        HashSet<Memory<int>> solutions)
    {
        foreach (var transformation in GetSymmetricalSolutions(solution))
            if (solutions.Contains(new Memory<int>(transformation)))
                return true;

        return false;
    }

    /// <summary>
    /// Alternate memory-based symmetry presence check (allocates 7 arrays per call).</summary>
    public static bool IsSymmetricalSolution(
        Memory<int> solution,
        HashSet<Memory<int>> solutions)
    {
        var span = solution.Span;
        foreach (var transformation in GetSymmetricalSolutions(span))
        {
            if (solutions.Contains(new Memory<int>(transformation)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Generates all 8 symmetrical variants for a given solution (including the identity/original).
    /// </summary>
    public static List<int[]> GetSymmetricalSolutions(int[] solution)
    {
        int n = solution.Length;
        var results = new List<int[]>(8);
        // 1. Identity
        results.Add((int[])solution.Clone());
        // 2. Rotate 90
        var rot90 = new int[n];
        for (int i = 0; i < n; i++) rot90[solution[i]] = n - 1 - i;
        results.Add(rot90);
        // 3. Rotate 180
        var rot180 = new int[n];
        for (int i = 0; i < n; i++) rot180[n - 1 - i] = n - 1 - solution[i];
        results.Add(rot180);
        // 4. Rotate 270
        var rot270 = new int[n];
        for (int i = 0; i < n; i++) rot270[n - 1 - solution[i]] = i;
        results.Add(rot270);
        // 5. Reflect vertical
        var reflVert = new int[n];
        for (int i = 0; i < n; i++) reflVert[n - 1 - i] = solution[i];
        results.Add(reflVert);
        // 6. Reflect horizontal
        var reflHoriz = new int[n];
        for (int i = 0; i < n; i++) reflHoriz[i] = n - 1 - solution[i];
        results.Add(reflHoriz);
        // 7. Reflect main diagonal
        var reflMainDiag = new int[n];
        for (int i = 0; i < n; i++) reflMainDiag[solution[i]] = i;
        results.Add(reflMainDiag);
        // 8. Reflect anti-diagonal
        var reflAntiDiag = new int[n];
        for (int i = 0; i < n; i++) reflAntiDiag[n - 1 - solution[i]] = n - 1 - i;
        results.Add(reflAntiDiag);
        return results;
    }

    /// <summary>
    /// Overload: Generates all 8 symmetrical variants for a given solution from ReadOnlySpan<int>.
    /// </summary>
    public static List<int[]> GetSymmetricalSolutions(ReadOnlySpan<int> solution)
    {
        return GetSymmetricalSolutions(solution.ToArray());
    }

    /// <summary>
    /// Overload: Generates all 8 symmetrical variants for a given solution from Memory<int>.
    /// </summary>
    public static List<int[]> GetSymmetricalSolutions(Memory<int> solution)
    {
        return GetSymmetricalSolutions(solution.ToArray());
    }

    /// <summary>
    /// Returns the canonical form (lexicographically smallest) of a solution among all its symmetry transformations.
    /// Used for efficient uniqueness checks.
    /// </summary>
    public static int[] GetCanonicalForm(int[] solution)
    {
        ArgumentNullException.ThrowIfNull(solution);
        int n = solution.Length;
        int[] min = null!;
        foreach (var trans in GetSymmetricalSolutions(solution))
        {
            if (min == null)
            {
                min = (int[])trans.Clone();
                continue;
            }
            bool isLess = false;
            for (int i = 0; i < n; i++)
            {
                if (trans[i] < min[i]) { isLess = true; break; }
                if (trans[i] > min[i]) break;
            }
            if (isLess)
            {
                for (int i = 0; i < n; i++) min[i] = trans[i];
            }
        }
        return min;
    }

    // --- Additional helpers (retained) ---
    /// <summary>
    /// Builds a title for solution listings with truncation / mode awareness.</summary>
    public static string SolutionTitle(SolutionMode solutionMode, ulong noOfSolutions)
    {
        if (solutionMode == SolutionMode.Single)
        { return "Solution:"; }

        if (noOfSolutions <= SimulationSettings.MaxNoOfSolutionsInOutput)
        {
            return solutionMode == SolutionMode.All
                ? "List of All Solutions (Included Symmetrical Ones):"
                : "List of Unique Solutions (Excluded Symmetrical Ones):";
        }

        return solutionMode == SolutionMode.All
            ? $"List of First {SimulationSettings.MaxNoOfSolutionsInOutput} Solution(s), May Include Symmetrical Ones:"
            : $"List of First {SimulationSettings.MaxNoOfSolutionsInOutput} Unique Solution(s), Excluded Symmetrical Ones:";
    }

    /// <summary>Returns a rotated solution (creates a new array).</summary>
    public static int[] Rotate(int[] solution, int degrees)
    {
        int n = solution.Length;
        int[] rotated = new int[n];

        switch (degrees)
        {
            case 90:
                for (int i = 0; i < n; i++)
                    rotated[solution[i]] = n - 1 - i;
                break;

            case 180:
                for (int i = 0; i < n; i++)
                    rotated[n - 1 - i] = n - 1 - solution[i];
                break;

            case 270:
                for (int i = 0; i < n; i++)
                    rotated[n - 1 - solution[i]] = i;
                break;

            default:
                throw new ArgumentException($"Invalid rotation angle: {degrees}. Only 90, 180, and 270 are supported.");
        }
        return rotated;
    }

    /// <summary>Returns a new reflected solution across the specified axis.</summary>
    public static int[] Reflect(int[] solution, string axis)
    {
        int n = solution.Length;
        int[] reflected = new int[n];

        switch (axis.ToLower())
        {
            case "horizontal":
                for (int i = 0; i < n; i++)
                    reflected[i] = n - 1 - solution[i];
                break;

            case "vertical":
                for (int i = 0; i < n; i++)
                    reflected[n - 1 - i] = solution[i];
                break;

            case "diagonal-primary":
                for (int i = 0; i < n; i++)
                    reflected[solution[i]] = i;
                break;

            case "diagonal-secondary":
                for (int i = 0; i < n; i++)
                    reflected[n - 1 - solution[i]] = n - 1 - i;
                break;

            default:
                throw new ArgumentException($"Invalid reflection axis: {axis}. Only 'horizontal', 'vertical', 'diagonal-primary', and 'diagonal-secondary' are supported.");
        }
        return reflected;
    }

    /// <summary>
    /// Legacy wrapper for compatibility: returns all 8 symmetrical transformations from ReadOnlySpan<int>.
    /// </summary>
    public static IEnumerable<int[]> GetSymmetricalTransformations(ReadOnlySpan<int> solution)
    {
        return GetSymmetricalSolutions(solution);
    }

    /// <summary>
    /// Legacy wrapper for compatibility: returns all 8 symmetrical transformations from Memory<int>.
    /// </summary>
    public static IEnumerable<int[]> GetSymmetricalTransformations(Memory<int> solution)
    {
        return GetSymmetricalSolutions(solution);
    }
}
