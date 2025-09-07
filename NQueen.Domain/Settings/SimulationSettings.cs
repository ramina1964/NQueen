namespace NQueen.Domain.Settings;

public static class SimulationSettings
{
    public const int MaxNoOfSolutionsInOutput = 50;
    public const int DefaultDelayInMilliseconds = 70;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    public static int ProgressThresholdPct { get; set; } = 5;

    public const int ProgressIntervalInMilliSec = 5000;
}
