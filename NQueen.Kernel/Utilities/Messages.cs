namespace NQueen.Kernel.Utilities;

public static class Messages
{
    public const string BoardSizeFormatError = "Board size must be a positive integer.";
    public const string NoSolutionMessage = "No Solutions found. Try a larger board size!";
    public const string ValueNullOrWhiteSpaceMsg =
        "Board size: Must be not null, empty or exclusively White Spaces.";

    public static string SingleSizeOutOfRangeMsg =>
        $"Enter Board Size for 'Single Solution's' inside: [1, {BoardSettings.MaxBoardSizeInSingleSolution}].";

    public static string UniqueSizeOutOfRangeMsg =>
        $"Enter Board Size for 'Unique Solutions' inside: [1, {BoardSettings.MaxBoardSizeInUniqueSolutions}].";

    public static string AllSizeOutOfRangeMsg =>
        $"Enter Board Size for 'All Solutions' inside: [1, {BoardSettings.MaxBoardSizeInAllSolutions}].";
}
