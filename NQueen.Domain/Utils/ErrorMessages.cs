namespace NQueen.Domain.Utils;

public static class ErrorMessages
{
    public const string InvalidIntegerError =
        "Board size must be a valid integer.";

    public const string NoSolutionMessage =
        "No Solutions found. Try a larger board size!";

    public const string ValueNullOrWhiteSpaceMsg =
        "Board size can not be null, empty or contain exclusively spaces.";

    public static string SizeTooSmallMsg =>
        $"Board size must be greater than or equal to {BoardSettings.MinSize}.";

    public static string SizeTooLargeForSingle =>
        $"Board size for Single Solution must not exceed {BoardSettings.MaxSizeForSingle}.";

    public static string SizeTooLargeForUnique =>
        $"Board size for 'Unique Solutions' must not exceed {BoardSettings.MaxSizeForUnique}.";

    public static string SizeTooLargeForAll =>
        $"Board size for 'All Solutions' must not exceed {BoardSettings.MaxSizeForAll}.";

    // DisplayMode.Visualize limit message
    public static string VisualizeSizeTooLarge =>
        $"Visualization is only supported for boards up to {SimulationSettings.MaxVisualizeBoardSize}. Switch to Hide mode for larger boards.";

    public static string GetTimeoutMessage(TimeSpan timeout) =>
        $"Condition was not met within the timeout period of {timeout.TotalSeconds} seconds.";
}
