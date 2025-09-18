namespace NQueen.Domain.Utils;

public static class SymmetryHelper
{
    // Helper method to generate all symmetrical transformations of a solution
    public static IEnumerable<int[]> GetSymmetricalTransformations(int[] solution)
    {
        // Delegate the calculation of symmetrical transformations to SymmetryHelper
        var symmetricalSolutions = SymmetryHelper.GetSymmetricalSolutions(solution);

        // Log transformations for debugging
        Debug.WriteLine("Generated symmetrical transformations:");
        foreach (var transformation in symmetricalSolutions)
            Debug.WriteLine(string.Join(",", transformation));

        return symmetricalSolutions;
    }

    // --- Version 1: Used by NQueen.Domain.Utils.SymmetryPruning (Memory<int> based) ---

    /// <summary>
    /// Returns all symmetrical transformations of a solution (Memory&lt;int&gt; version).
    /// </summary>
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

    // --- Version 2: Used by NQueen.Kernel.Solvers.SolverEngine (int[] based) ---

    /// <summary>
    /// Returns all symmetrical transformations of a solution (int[] version).
    /// </summary>
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

    /// <summary>
    /// Used by SolverEngine: Checks if the solution or any of its symmetrical transformations already exists in the Solutions collection.
    /// </summary>
    public static bool IsSymmetricalSolution(
        Memory<int> solution,
        HashSet<Memory<int>> solutions)
    {
        var original = solution.Span;
        var transformations = GetSymmetricalTransformations(original.ToArray());

        foreach (var transformation in transformations)
        {
            if (solutions.Contains(new Memory<int>(transformation)))
            {
                Debug.WriteLine($"Symmetry detected: {string.Join(",", transformation)} matches an existing solution.");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the solution or any of its symmetrical transformations already exists in the set (Memory&lt;int&gt; version).
    /// </summary>
    public static bool IsSymmetrical(
        Memory<int> solution,
        HashSet<Memory<int>> solutions)
    {
        foreach (var transformation in GetSymmetricalSolutions(solution))
        {
            if (solutions.Contains(transformation))
                return true;
        }

        return false;
    }

    // --- Additional helpers (unchanged) ---
    public static string SolutionTitle(SolutionMode solutionMode, int noOfSolutions)
    {
        if (solutionMode == SolutionMode.Single)
        { return "Solution:"; }

        if (noOfSolutions <= SimulationSettings.MaxNoOfSolutionsInOutput)
        {
            return solutionMode == SolutionMode.All
             ? $"List of All Solutions (Included Symmetrical Ones):"
             : $"List of Unique Solutions (Excluded Symmetrical Ones):";
        }

        // Here is: NoOfSolutions > MaxNoOfSolutionsInOutput
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

            case "diagonal-primary": // Lower-left to upper-right
                for (int i = 0; i < n; i++)
                    reflected[solution[i]] = i;
                break;

            case "diagonal-secondary": // Lower-right to upper-left
                for (int i = 0; i < n; i++)
                    reflected[n - 1 - solution[i]] = n - 1 - i;
                break;

            default:
                throw new ArgumentException($"Invalid reflection axis: {axis}. Only 'horizontal', 'vertical', 'diagonal-primary', and 'diagonal-secondary' are supported.");
        }

        return reflected;
    }
}
