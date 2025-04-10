namespace NQueen.Kernel.Utilities;

public static class Utility
{
    //public const int ByteMaxValue = 255;

    public const int DefaultBoardSize = 8;
    public const int DefaultDelayInMilliseconds = 500;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    public const int MaxNoOfSolutionsInOutput = 50;
    public const int RelativeFactor = 8;
    public const int MinBoardSize = 1;

    public const int SmallBoardSizeForUniqueSolutions = 10;
    public const int MediumBoardSizeForUniqueSolutions = 15;

    public const int MaxBoardSizeForSingleSolution = 37;

    // Todo: Set back these constants to 17, if unsuccessful.
    public const int MaxBoardSizeForUniqueSolutions = 18;
    public const int MaxBoardSizeForAllSolutions = 18;

    // This indicates the frequency of progrssbar update based on the board size value.
    // Todo: Use constants here.
    public static int SolutionCountPerUpdate(int boardSize) =>
        boardSize <= SmallBoardSizeForUniqueSolutions
        ? 5
        : boardSize <= MediumBoardSizeForUniqueSolutions
        ? 1_000 :
        100_000;

    public const string InvalidSByteError =
        "Board size must be a valid integer.";

    public const string NoSolutionMessage =
        "No Solutions found. Try a larger board size!";

    public const string ValueNullOrWhiteSpaceMsg =
        "Board size can not be null, empty or contain exclusively spaces.";

    public static string SizeTooSmallMsg =>
        $"Board size must be greater than or equal to {MinBoardSize}.";

    public static string SizeTooLargeForSingleSolutionMsg =>
        $"Board size for single solution must not exceed {MaxBoardSizeForSingleSolution}.";

    public static string SizeTooLargeForUniqueSolutionsMsg =>
        $"Board size for unique solutions must not exceed {MaxBoardSizeForUniqueSolutions}.";

    public static string SizeTooLargeForAllSolutionsMsg =>
        $"Board size for all solutions must not exceed {MaxBoardSizeForAllSolutions}.";

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

    public static string SolutionTitle(SolutionMode solutionMode)
    {
        return solutionMode switch
        {
            SolutionMode.Single => "No. of Solutions",
            SolutionMode.Unique => $"No. of Unique Solutions",
            SolutionMode.All => $"No. of All Solutions",
            _ => throw new MissingFieldException("Non-Existent Enum Value!"),
        };
    }

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
            _ => throw new ArgumentOutOfRangeException(SizeTooLargeForUniqueSolutionsMsg)
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
            _ => throw new ArgumentOutOfRangeException(SizeTooLargeForAllSolutionsMsg)
        };
    #endregion PrivateMembers
}
