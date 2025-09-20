namespace NQueen.Domain.Utils;

public static partial class SymmetryHelper
{
    // --- New span-based overload to avoid allocating an intermediate int[] when only a Memory/Span is available ---
    // Changed from iterator (IEnumerable with yield) to materializing List<int[]> because ReadOnlySpan<T> (ref struct)
    // cannot be used in an iterator method (CS4007).
    public static List<int[]> GetSymmetricalSolutions(ReadOnlySpan<int> solution)
    {
        int boardSize = solution.Length;

        var symmToMidHorizontal = new int[boardSize];
        var symmToMidVertical   = new int[boardSize];
        var symmToMainDiag      = new int[boardSize];
        var symmToBiDiag        = new int[boardSize];
        var counter90           = new int[boardSize];
        var counter180          = new int[boardSize];
        var counter270          = new int[boardSize];

        for (int rowIndex = 0; rowIndex < boardSize; rowIndex++)
        {
            int flippedRowIndex = boardSize - rowIndex - 1;
            int flippedColIndex = boardSize - solution[rowIndex] - 1;

            symmToMidHorizontal[flippedRowIndex] = solution[rowIndex];
            counter90[flippedColIndex] = symmToMainDiag[solution[rowIndex]] = rowIndex;

            counter180[flippedRowIndex] =
                symmToMidVertical[rowIndex] = flippedColIndex;

            counter270[solution[rowIndex]] =
                symmToBiDiag[flippedColIndex] = flippedRowIndex;
        }

        return new List<int[]>(7)
        {
            symmToMidVertical,
            symmToMidHorizontal,
            symmToMainDiag,
            symmToBiDiag,
            counter90,
            counter180,
            counter270
        };
    }

    // --- Version 1 (Memory<int> based) used by symmetry pruning logic ---
    public static List<Memory<int>> GetSymmetricalSolutions(Memory<int> solution)
    {
        var boardSize = solution.Length;

        var symmToMidHorizontal = new Memory<int>(new int[boardSize]);
        var symmToMidVertical = new Memory<int>(new int[boardSize]);
        var symmToMainDiag = new Memory<int>(new int[boardSize]);
        var symmToBiDiag = new Memory<int>(new int[boardSize]);
        var counter90 = new Memory<int>(new int[boardSize]);
        var counter180 = new Memory<int>(new int[boardSize]);
        var counter270 = new Memory<int>(new int[boardSize]);
        var solutionSpan = solution.Span;

        for (var rowIndex = 0; rowIndex < boardSize; rowIndex++)
        {
            var flippedRowIndex = boardSize - rowIndex - 1;
            var flippedColIndex = boardSize - solutionSpan[rowIndex] - 1;

            symmToMidHorizontal.Span[flippedRowIndex] = solutionSpan[rowIndex];
            counter90.Span[flippedColIndex] = symmToMainDiag.Span[solutionSpan[rowIndex]] = rowIndex;

            counter180.Span[flippedRowIndex] =
                symmToMidVertical.Span[rowIndex] = flippedColIndex;

            counter270.Span[solutionSpan[rowIndex]] =
                symmToBiDiag.Span[flippedColIndex] = flippedRowIndex;
        }

        return
        [
            symmToMidVertical,
            symmToMidHorizontal,
            symmToMainDiag,
            symmToBiDiag,
            counter90,
            counter180,
            counter270
        ];
    }

    // --- Version 2 (int[] based) used by solver engine ---
    public static HashSet<int[]> GetSymmetricalSolutions(int[] solution)
    {
        var boardSize = solution.Length;

        var symmetricalToMidHorizontal = new int[boardSize];
        var symmetricalToMidVertical = new int[boardSize];
        var symmetricalToMainDiag = new int[boardSize];
        var symmetricalToBiDiag = new int[boardSize];
        var rotatedCounter90 = new int[boardSize];
        var rotatedCounter180 = new int[boardSize];
        var rotatedCounter270 = new int[boardSize];

        for (var rowIndex = 0; rowIndex < boardSize; rowIndex++)
        {
            var flippedRowIndex = boardSize - rowIndex - 1;
            var flippedColIndex = boardSize - solution[rowIndex] - 1;

            symmetricalToMidHorizontal[flippedRowIndex] = solution[rowIndex];
            rotatedCounter90[flippedColIndex] = symmetricalToMainDiag[solution[rowIndex]] = rowIndex;

            rotatedCounter180[flippedRowIndex] =
                symmetricalToMidVertical[rowIndex] = flippedColIndex;

            rotatedCounter270[solution[rowIndex]] =
                symmetricalToBiDiag[flippedColIndex] = flippedRowIndex;
        }

        return new HashSet<int[]>(new IntArrayComparer())
        {
            symmetricalToMidVertical,
            symmetricalToMidHorizontal,
            symmetricalToMainDiag,
            symmetricalToBiDiag,
            rotatedCounter90,
            rotatedCounter180,
            rotatedCounter270,
        };
    }

    // Retained existing API: Get all symmetrical transformations (int[] entry point)
    public static IEnumerable<int[]> GetSymmetricalTransformations(int[] solution)
    {
        var symmetricalSolutions = GetSymmetricalSolutions(solution);

        return symmetricalSolutions;
    }

    /// <summary>
    /// Add a solution to a uniqueness set if none of its symmetrical transformations exist.
    /// This avoids allocating 7 temporary arrays + HashSet per check by reusing a single scratch buffer.
    /// Returns true if the (original) solution was added as unique; false if any symmetry already existed.
    /// </summary>
    public static bool AddIfUnique(int[] solution, HashSet<int[]> uniqueSolutions, int[] scratch)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(uniqueSolutions);
        ArgumentNullException.ThrowIfNull(scratch);
        if (scratch.Length < solution.Length)
            throw new ArgumentException("Scratch buffer too small", nameof(scratch));

        if (uniqueSolutions.Contains(solution))
            return false;

        int n = solution.Length;

        // 1. Vertical reflection: row i -> column n-1 - solution[i]
        for (int i = 0; i < n; i++)
            scratch[i] = n - 1 - solution[i];
        if (uniqueSolutions.Contains(scratch)) return false;

        // 2. Horizontal reflection: row (n-1 - i) = solution[i]
        for (int i = 0; i < n; i++)
            scratch[n - 1 - i] = solution[i];
        if (uniqueSolutions.Contains(scratch)) return false;

        // 3. Main diagonal reflection: row solution[i] = i
        for (int i = 0; i < n; i++)
            scratch[solution[i]] = i;
        if (uniqueSolutions.Contains(scratch)) return false;

        // 4. Anti-diagonal reflection: row (n-1 - solution[i]) = n-1 - i
        for (int i = 0; i < n; i++)
            scratch[n - 1 - solution[i]] = n - 1 - i;
        if (uniqueSolutions.Contains(scratch)) return false;

        // 5. Rot 90: row (n-1 - solution[i]) = i
        for (int i = 0; i < n; i++)
            scratch[n - 1 - solution[i]] = i;
        if (uniqueSolutions.Contains(scratch)) return false;

        // 6. Rot 180: row (n-1 - i) = n-1 - solution[i]
        for (int i = 0; i < n; i++)
            scratch[n - 1 - i] = n - 1 - solution[i];
        if (uniqueSolutions.Contains(scratch)) return false;

        // 7. Rot 270: row solution[i] = n-1 - i
        for (int i = 0; i < n; i++)
            scratch[solution[i]] = n - 1 - i;
        if (uniqueSolutions.Contains(scratch)) return false;

        uniqueSolutions.Add((int[])solution.Clone());
        return true;
    }

    /// <summary>
    /// Used by SolverEngine: Checks if any symmetrical transformation already exists in the set (Memory<int> version).
    /// Now uses span overload to avoid one allocation.
    /// </summary>
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
    /// Checks if any symmetrical transformation already exists (Memory<int> direct version).
    /// </summary>
    public static bool IsSymmetrical(
        Memory<int> solution,
        HashSet<Memory<int>> solutions)
    {
        foreach (var transformation in GetSymmetricalSolutions(solution))
            if (solutions.Contains(transformation))
                return true;

        return false;
    }

    // --- Additional helpers (retained) ---
    public static string SolutionTitle(SolutionMode solutionMode, int noOfSolutions)
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
}
