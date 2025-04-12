namespace NQueen.Kernel.Utilities;

// Todo: Bug: Even with 0 as DefaultDelayInMIlliSeconds, simulation stops after the first quuen is placed.

public static class SolverHelper
{
    public const double StartProgressValue = 0;

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

        for (var j = 0; j < boardSize; j++)
        {
            var index1 = boardSize - j - 1;
            var index2 = boardSize - solution[j] - 1;

            symmetricalToMidHorizontal[index1] = solution[j];
            rotatedCounter90[index2] = symmetricalToMainDiag[solution[j]] = j;
            rotatedCounter180[index1] = symmetricalToMidVertical[j] = index2;
            rotatedCounter270[solution[j]] = symmetricalToBiDiag[index2] = index1;
        }

        return new HashSet<int[]>(new SequenceEquality<int>())
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

    public static int FindSolutionSize(int boardSize, SolutionMode solutionMode) =>
        solutionMode == SolutionMode.Single
            ? 1
            : solutionMode == SolutionMode.Unique
            ? GetSolutionSizeUnique(boardSize)
            : GetSolutionSizeAll(boardSize);

    public static string SolutionTitle(SolutionMode solutionMode) =>
        (solutionMode == SolutionMode.Single)
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

    #region PrivateMembers
    private static int GetSolutionSizeUnique(int boardSize) =>
        boardSize switch
        {
            1 => 1,
            2 => 0,
            3 => 0,
            4 => 1,
            5 => 2,
            6 => 1,
            7 => 6,
            8 => 12,
            9 => 46,
            10 => 92,
            11 => 341,
            12 => 1787,
            13 => 9233,
            14 => 45752,
            15 => 285053,
            16 => 1846955,
            17 => 11977939,
            _ => throw new ArgumentOutOfRangeException(Messages.SizeTooLargeForUniqueSolutionsMsg)
        };

    private static int GetSolutionSizeAll(int boardSize) =>
        boardSize switch
        {
            1 => 1,
            2 => 0,
            3 => 0,
            4 => 2,
            5 => 10,
            6 => 4,
            7 => 40,
            8 => 92,
            9 => 352,
            10 => 724,
            11 => 2680,
            12 => 14200,
            13 => 73712,
            14 => 365596,
            15 => 2279184,
            16 => 14772512,
            17 => 95815104,
            _ => throw new ArgumentOutOfRangeException(Messages.SizeTooLargeForAllSolutionsMsg)
        };
    #endregion PrivateMembers
}
