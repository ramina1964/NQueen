namespace NQueen.Kernel.Utilities;

public static class Messages
{
    public const string InvalidByteError = "Board size must be a whole number inside [1 and 255].";
    public const string NoSolutionMessage = "No Solutions found. Try a larger board size!";
    public const string ValueNullOrWhiteSpaceMsg =
        "Board size can not be null, empty or contain exclusively spaces.";

    public static string SizeTooSmallMsg =>
        $"Board size must be greater than or equal to {BoardSettings.MinBoardSize}.";

    public static string SingleSizeOutOfRangeMsg =>
        $"Single Solutions' board size must be inside [1, {BoardSettings.MaxBoardSizeForSingleSolution}].";

    public static string UniqueSizeOutOfRangeMsg =>
        $"Unique Solutions' board size must be inside [1, {BoardSettings.MaxBoardSizeForUniqueSolutions}].";

    public static string AllSizeOutOfRangeMsg =>
        $"All Solutions' board size must be inside [1, {BoardSettings.MaxBoardSizeForAllSolutions}].";
}
