namespace NQueen.Kernel.Utilities;

public static class Messages
{
    public const string InvalidByteError = "Enter board size inside: [1, 255].";
    public const string NoSolutionMessage = "No Solutions found. Try a larger board size!";
    public const string ValueNullOrWhiteSpaceMsg =
        "Board size: Not null, Empty or Exclusively White Spaces.";

    public static string SizeTooSmallMsg =>
        $"Enter board size inside: Board Size >= {BoardSettings.MinBoardSize}";

    public static string SingleSizeOutOfRangeMsg =>
        $"Enter board size inside: [1, {BoardSettings.MaxBoardSizeInSingleSolution}].";

    public static string UniqueSizeOutOfRangeMsg =>
        $"Enter board size inside: [1, {BoardSettings.MaxBoardSizeInUniqueSolutions}].";

    public static string AllSizeOutOfRangeMsg =>
        $"Enter board size inside: [1, {BoardSettings.MaxBoardSizeInAllSolutions}].";
}
