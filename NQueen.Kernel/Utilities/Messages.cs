namespace NQueen.Kernel.Utilities;

public static class Messages
{
    public const string SizeOutOfRangeError = "Enter board size inside: [1, 255].";
    public const string NoSolutionMessage = "No Solutions found. Try a larger board size!";
    public const string ValueNullOrWhiteSpaceMsg =
        "Board size: Must not be null, empty or exclusively white space.";

    public static string SingleSizeOutOfRangeMsg =>
        $"Enter Board Size for 'Single Solution' inside: [1, {BoardSettings.MaxBoardSizeInSingleSolution}].";

    public static string UniqueSizeOutOfRangeMsg =>
        $"Enter Board Size for 'Unique Solutions' inside: [1, {BoardSettings.MaxBoardSizeInUniqueSolutions}].";

    public static string AllSizeOutOfRangeMsg =>
        $"Enter Board Size for 'All Solutions' inside: [1, {BoardSettings.MaxBoardSizeInAllSolutions}].";
}
