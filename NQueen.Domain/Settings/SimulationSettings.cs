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
        ResultStorageMode.Materialize;

    public const ResultStorageMode DefaultUniqueStorageMode =
        ResultStorageMode.Materialize;

    // Threshold size where we reduce QueenPlaced event frequency.
    public const int QueenPlacedSamplingThresholdSize = 12;

    // Sample rate for large boards (>= threshold).
    public const int QueenPlacedLargeBoardSampleRate = 1_000;

    public static int ProgressThresholdPct { get; set; } = 5;

    // Heartbeat interval (minimum time between forced progress UI updates when solver is quiet)
    public const int ProgressIntervalInMilliSec = 10_000;

    // Visualization is allowed only up to and including this board size.
    public const int MaxVisualizeBoardSize = 6;

    // Threshold at/above which we throttle parallel progress updates for All modes.
    public const int LargeBoardProgressThrottleThreshold = 16;

    // Threshold at/above which symmetry-pruned unique counting is used for Unique+CountOnly mode.
    public const int LargeBoardSymmetryPruningThreshold = 15;

    // New constant for dynamic root split limit
    public const int DynamicRootSplitLimitN = 19;

    public const int AdaptiveRootMultiplier = 8; // heuristic multiplier for root frame target versus logical cores
    public const int RootBranchThreshold = 4; // branch factor threshold guiding adaptive root expansion
    public const int WeightLookaheadDepth = 3; // depth of lookahead when estimating root weights

    public static int CalculateSplitDepth(int boardSize) =>
        Math.Max(1, boardSize / Environment.ProcessorCount);
}
