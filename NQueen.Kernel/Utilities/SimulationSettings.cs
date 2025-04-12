namespace NQueen.Kernel.Utilities;

public class SimulationSettings
{
    public const int MaxNoOfSolutionsInOutput = 50;
    public const int DefaultDelayInMilliseconds = 0;
    public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    // This indicates the frequency of progrssbar update based on the board size value.
    // Todo: Use constants here.
    public static int SolutionCountPerUpdate(int boardSize) =>
        boardSize <= BoardSettings.SmallSizeForUniqueMode
        ? 5
        : boardSize <= BoardSettings.MediumSizeForUniqueMode
        ? 1_000 :
        100_000;
}
