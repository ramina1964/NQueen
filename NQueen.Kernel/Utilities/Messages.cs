namespace NQueen.Kernel.Utilities;

public static class Messages
{
    public const string InvalidSByteError = "Board size must be a valid integer.";
    public const string NoSolutionMessage = "No Solutions found. Try a larger board size!";
    public const string ValueNullOrWhiteSpaceMsg = "Board size can not be null, empty or contain exclusively spaces.";

    public static string SizeTooSmallMsg =>
        $"Board size must be greater than or equal to {BoardSettings.MinBoardSize}.";

    public static string SizeTooLargeForSingleSolutionMsg =>
        $"Board size for single solution must not exceed {BoardSettings.MaxBoardSizeForSingleSolution}.";

    public static string SizeTooLargeForUniqueSolutionsMsg =>
        $"Board size for unique solutions must not exceed {BoardSettings.MaxBoardSizeForUniqueSolutions}.";

    public static string SizeTooLargeForAllSolutionsMsg =>
        $"Board size for all solutions must not exceed {BoardSettings.MaxBoardSizeForAllSolutions}.";
}
