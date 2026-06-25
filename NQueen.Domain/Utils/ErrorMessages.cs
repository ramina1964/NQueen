namespace NQueen.Domain.Utils;

public static class ErrorMessages
{
    public const string InvalidIntegerError =
        "Invalid Board Size: Must be an integer.";

    public const string NoSolutionMsg =
        "No Solutions Found: Try a larger board size!";

    public const string ValueNullOrWhiteSpaceMsg =
        "Invalid Board Size: null, empty or spaces.";

    public static string OutOfRangeMsg =>
        $"Valid Range is [{BoardSettings.MinSize}, {BoardSettings.MaxSizeForAll}].";

    public static string OutOfRangeSingle =>
        $"Valid Size: [{BoardSettings.MinSize}, {BoardSettings.MaxSizeForSingle}].";

    public static string OutOfRangeUnique =>
        $"Valid Size: [{BoardSettings.MinSize}, {BoardSettings.MaxSizeForUnique}].";

    public static string OutOfRangeAll =>
        $"Valid Size: [{BoardSettings.MinSize}, {BoardSettings.MaxSizeForAll}].";

    // DisplayMode.Visualize limit message
    public static string VisualizeSizeTooLarge =>
        $"Visualization is only supported for boards up to {SimulationSettings.MaxVisualizeBoardSize}. Switch to Hide mode for larger boards.";

    //public static string SizeTooLargeForUnique { get; set; }

    public static string GetTimeoutMessage(TimeSpan timeout) =>
        $"Condition was not met within the timeout period of {timeout.TotalSeconds} seconds.";
}
