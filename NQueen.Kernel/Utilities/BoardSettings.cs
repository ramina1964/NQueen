namespace NQueen.Kernel.Utilities;

public static class BoardSettings
{
    public const int DefaultBoardSize = 8;
    public const int RelativeFactor = 8;
    public const int MinSize = 1;

    public const int SmallSizeForUniqueMode = 10;
    public const int MediumSizeForUniqueMode = 15;

    public const int MaxSizeForSingleMode = 37;

    // Todo: Set back these constants to 17, if unsuccessful.
    public const int MaxSizeForUniqueMode = 18;
    public const int MaxSizeForAllMode = 18;

    //public const int MaxNoOfSolutionsInOutput = 50;
    //public const int DefaultDelayInMilliseconds = 0;
    //public const SolutionMode DefaultSolutionMode = SolutionMode.Unique;
    //public const DisplayMode DefaultDisplayMode = DisplayMode.Hide;

    //// This indicates the frequency of progrssbar update based on the board size value.
    //// Todo: Use constants here.
    //public static int SolutionCountPerUpdate(int boardSize) =>
    //    boardSize <= SmallSizeForUniqueMode
    //    ? 5
    //    : boardSize <= MediumSizeForUniqueMode
    //    ? 1_000 :
    //    100_000;
}
