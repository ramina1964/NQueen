namespace NQueen.Shared;

public static class Utility
{
    public const sbyte DefaultBoardSize = 8;
    public const int DefaultDelayInMilliseconds = 500;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    public const int MaxNoOfSolutionsInOutput = 50;
    public const sbyte RelativeFactor = 8;
    public const sbyte MinBoardSize = 1;

    public const int SmallBoardSizeForUniqueSolutions = 10;
    public const int MediumBoardSizeForUniqueSolutions = 15;

    public const sbyte MaxBoardSizeForSingleSolution = 37;

    // Todo: Set back these constants to 17, if unsuccessful.
    public const sbyte MaxBoardSizeForUniqueSolutions = 18;
    public const sbyte MaxBoardSizeForAllSolutions = 18;

    // This property indicates who often we update the progreebar value as a function of the board size.
    // Todo: Use constants here.
    public static int SolutionCountPerUpdate(sbyte boardSize) =>
        (boardSize <= SmallBoardSizeForUniqueSolutions)
        ? 5
        : (boardSize <= MediumBoardSizeForUniqueSolutions)
        ? 1_000 :
        100_000;

    public static string InvalidSByteError => $"Board size must be a valid integer.";

    public static string NoSolutionMessage => $"No Solutions found. Try a larger board size!";

    public static string ValueNullOrWhiteSpaceMsg => $"Board size can not be null, empty or contain exclusively spaces.";

    public static string SizeTooSmallMsg => $"Board size must be greater than or equal to {MinBoardSize}.";

    public static string SizeTooLargeForSingleSolutionMsg => $"Board size for single solution must not exceed {MaxBoardSizeForSingleSolution}.";

    public static string SizeTooLargeForUniqueSolutionsMsg => $"Board size for unique solutions must not exceed {MaxBoardSizeForUniqueSolutions}.";

    public static string SizeTooLargeForAllSolutionsMsg => $"Board size for all solutions must not exceed {MaxBoardSizeForAllSolutions}.";

    public static List<sbyte[]> GetSymmetricalSolutions(sbyte[] solution)
    {
        sbyte boardSize = (sbyte)solution.Length;

        sbyte[] symmetricalToMidHorizontal = new sbyte[boardSize];
        var symmetricalToMidVertical = new sbyte[boardSize];
        var symmetricalToMainDiag = new sbyte[boardSize];
        var symmetricalToBiDiag = new sbyte[boardSize];
        var rotatedCounter90 = new sbyte[boardSize];
        var rotatedCounter180 = new sbyte[boardSize];
        var rotatedCounter270 = new sbyte[boardSize];

        for (sbyte j = 0; j < boardSize; j++)
        {
            sbyte index1 = (sbyte)(boardSize - j - 1);
            sbyte index2 = (sbyte)(boardSize - solution[j] - 1);

            symmetricalToMidHorizontal[index1] = solution[j];
            rotatedCounter90[index2] = symmetricalToMainDiag[solution[j]] = j;
            rotatedCounter180[index1] = symmetricalToMidVertical[j] = index2;
            rotatedCounter270[solution[j]] = symmetricalToBiDiag[index2] = index1;
        }

        return new HashSet<sbyte[]>(new SequenceEquality<sbyte>())
            {
                symmetricalToMidVertical,
                symmetricalToMidHorizontal,
                symmetricalToMainDiag,
                symmetricalToBiDiag,
                rotatedCounter90,
                rotatedCounter180,
                rotatedCounter270,
            }.ToList();
    }

    public static int FindSolutionSize(sbyte boardSize, SolutionMode solutionMode) =>
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
    private static int GetSolutionSizeUnique(sbyte boardSize) =>
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

    private static int GetSolutionSizeAll(sbyte boardSize) =>
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
