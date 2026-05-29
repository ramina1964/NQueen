namespace NQueen.Domain.Settings;

public static class SimulationSettings
{
    // Maximum count of displayed solutions
    public const int MaxDisplayedCount = 5;
    public const int DefaultDelayInMilliseconds = 500;
    public const int MinDelayInMilliseconds = 5;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    // Global lookup threshold for using precomputed counts instead of enumeration
    public const int LookupThresholdN = 21;

    // Parallel tuning
    public const bool DefaultUseParallel = true;
    public const int DefaultParallelRootSplitDepth = 1;

    // Threshold to auto-enable parallel execution for All mode (materialize path)
    public const int ParallelAllMaterializeAutoEnableThresholdN = 14;

    // Threshold for enabling optimized Unique count-only execution path
    public const int UniqueCountOnlyParallelThresholdN = 16;

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
    public const int MaxVisualizeBoardSize = 10;

    // Threshold at/above which symmetry-pruned unique counting is used for Unique+CountOnly mode.
    public const int LargeBoardSymmetryPruningThreshold = 15;

    // Early prefix-pruning depth gate activation threshold for large boards
    public const int PrefixPruneEarlyThresholdN = 20;

    // Constructive sampling threshold for large boards (avoid heavy engine materialization)
    public const int ConstructiveSampleThresholdN = 20;

    // Start size for intermediate large-board verification tests (gap between slow and high board suites)
    public const int LargeBoardIntermediateStartSize = 15;
}
