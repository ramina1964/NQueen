namespace NQueen.Domain.Utils;

public static class SolverHelper
{
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

    public static string UpdateSolutionTitle(SolutionMode solutionMode) =>
        solutionMode == SolutionMode.Single
                ? $"Solution"
                : solutionMode == SolutionMode.Unique
                ? $"Unique Solutions (Max: {SimulationSettings.MaxNoOfSolutionsInOutput})"
                : $"All Solutions (Max: {SimulationSettings.MaxNoOfSolutionsInOutput})";

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
}
