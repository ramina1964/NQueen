namespace NQueen.Kernel.Utilities;

public static class Messages
{
    public const string InvalidSIntegerError =
    "Board size must be a valid integer.";

    public const string NoSolutionMessage =
        "No Solutions found. Try a larger board size!";

    public const string ValueNullOrWhiteSpaceMsg =
        "Board size can not be null, empty or contain exclusively spaces.";

    public static string SizeTooSmallMsg =>
        $"Board size must be greater than or equal to {BoardSettings.MinSize}.";

    public static string SizeTooLargeForSingleSolutionMsg =>
        $"Board size for single solution must not exceed {BoardSettings.MaxSizeForSingleMode}.";

    public static string SizeTooLargeForUniqueSolutionsMsg =>
        $"Board size for unique solutions must not exceed {BoardSettings.MaxSizeForUniqueMode}.";

    public static string SizeTooLargeForAllSolutionsMsg =>
        $"Board size for all solutions must not exceed {BoardSettings.MaxSizeForAllMode}.";
}
