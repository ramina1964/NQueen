namespace NQueen.Domain.Settings;

public static class SimulationSettings
{
    // Set to 0 (or negative) to mean "no UI cap" – UI will decide how many to display.
    public const int MaxNoOfSolutionsInOutput = 5;
    public const int DefaultDelayInMilliseconds = 70;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    public static int ProgressThresholdPct { get; set; } = 5;

    public const int ProgressIntervalInMilliSec = 5000;
}
