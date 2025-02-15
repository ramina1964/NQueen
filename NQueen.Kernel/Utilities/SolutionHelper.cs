namespace NQueen.Kernel.Utilities;

public static class SolutionHelper
{
    public const int DefaultDelayInMilliseconds = 500;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;
    public const int MaxNoOfSolutionsInOutput = 50;
    public const byte RelativeFactor = 8;

    public static HashSet<byte[]> GetSymmetricalSolutions(byte[] solution)
    {
        byte boardSize = (byte)solution.Length;

        byte[] symmetricalToMidHorizontal = new byte[boardSize];
        var symmetricalToMidVertical = new byte[boardSize];
        var symmetricalToMainDiag = new byte[boardSize];
        var symmetricalToBiDiag = new byte[boardSize];
        var rotatedCounter90 = new byte[boardSize];
        var rotatedCounter180 = new byte[boardSize];
        var rotatedCounter270 = new byte[boardSize];

        for (byte j = 0; j < boardSize; j++)
        {
            byte index1 = (byte)(boardSize - j - 1);
            byte index2 = (byte)(boardSize - solution[j] - 1);

            symmetricalToMidHorizontal[index1] = solution[j];
            rotatedCounter90[index2] = symmetricalToMainDiag[solution[j]] = j;
            rotatedCounter180[index1] = symmetricalToMidVertical[j] = index2;
            rotatedCounter270[solution[j]] = symmetricalToBiDiag[index2] = index1;
        }

        return new HashSet<byte[]>(new SequenceEquality<byte>())
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

    public static int FindSolutionSize(byte boardSize, SolutionMode solutionMode) =>
        solutionMode == SolutionMode.Single
            ? 1
            : solutionMode == SolutionMode.Unique
            ? GetSolutionSizeUnique(boardSize)
            : GetSolutionSizeAll(boardSize);

    public static string SolutionTitle(SolutionMode solutionMode) =>
        solutionMode switch
        {
            SolutionMode.Single => "No. of Solutions",
            SolutionMode.Unique => "No. of Unique Solutions",
            SolutionMode.All => "No. of All Solutions",
            _ => throw new MissingFieldException("Non-Existent Enum Value!")
        };

    public static string SolutionTitle(SolutionMode solutionMode, int noOfSolutions)
    {
        if (solutionMode == SolutionMode.Single)
        { return "Solution:"; }

        if (noOfSolutions <= MaxNoOfSolutionsInOutput)
        {
            return solutionMode == SolutionMode.All
             ? $"List of All Solutions (Included Symmetrical Ones):"
             : $"List of Unique Solutions (Excluded Symmetrical Ones):";
        }

        // Here is: NoOfSolutions > MaxNoOfSolutionsInOutput
        return solutionMode == SolutionMode.All
            ? $"List of First {MaxNoOfSolutionsInOutput} Solution(s), May Include Symmetrical Ones:"
            : $"List of First {MaxNoOfSolutionsInOutput} Unique Solution(s), Excluded Symmetrical Ones:";
    }

    #region PrivateMembers
    private static int GetSolutionSizeUnique(byte boardSize) =>
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

    private static int GetSolutionSizeAll(byte boardSize) =>
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

