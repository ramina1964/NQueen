namespace NQueen.Domain.Settings;

public static class SimulationSettings
{
    // Maximum count of displayed solutions
    public const int MaxDisplayedCount = 5;
    public const int DefaultDelayInMilliseconds = 70;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    // Parallel tuning
    public const bool DefaultUseParallel = true;
    public const int DefaultParallelRootSplitDepth = 1;

    // Storage strategies (changed to MaterializeSample to satisfy tests expecting sample solutions)
    public const ResultStorageMode DefaultAllStorageMode =
        ResultStorageMode.Materialize; // was CountOnly

    public const ResultStorageMode DefaultUniqueStorageMode =
        ResultStorageMode.Materialize; // was CountOnly

    // Threshold size where we reduce QueenPlaced event frequency.
    public const int QueenPlacedSamplingThresholdSize = 12;

    // Sample rate for large boards (>= threshold).
    public const int QueenPlacedLargeBoardSampleRate = 1000;

    public static int ProgressThresholdPct { get; set; } = 5;

    public const int ProgressIntervalInMilliSec = 5000;

    // Visualization is allowed only up to and including this board size.
    public const int MaxVisualizeBoardSize = 6;

    // NEW: Threshold at/above which we throttle parallel progress updates for All modes.
    public const int LargeBoardProgressThrottleThreshold = 16;
}
