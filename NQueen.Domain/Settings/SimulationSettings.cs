namespace NQueen.Domain.Settings;

public static class SimulationSettings
{
    public const int MaxNoOfSolutionsInOutput = 5;
    public const int DefaultDelayInMilliseconds = 70;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    // Minimum board size where parallelization is considered.
    public const int ParallelMinBoardSize = 10;

    // Threshold size where we reduce QueenPlaced event frequency.
    public const int QueenPlacedSamplingThresholdSize = 12;

    // Sample rate for large boards (>= threshold).
    public const int QueenPlacedLargeBoardSampleRate = 1000;

    public static int ProgressThresholdPct { get; set; } = 5;

    public const int ProgressIntervalInMilliSec = 5000;
}
