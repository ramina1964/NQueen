namespace NQueen.Domain.Utils;

public static class SymmetryHelper
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
