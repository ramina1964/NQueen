namespace NQueen.Domain.Settings;

public static class SimulationSettings
{
    public const int MaxNoOfSolutionsInOutput = 50;
    public const int DefaultDelayInMilliseconds = 70;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    public static int SolutionCountPerUpdate(int boardSize) =>
        boardSize <= BoardSettings.SmallSizeForUniqueMode
        ? 5
        : boardSize <= BoardSettings.MediumSizeForUniqueMode
        ? 1_000
        : boardSize <= BoardSettings.RelativeLargeSizeForUniqueMode
        ? 100_000
        : 500_000;
}
