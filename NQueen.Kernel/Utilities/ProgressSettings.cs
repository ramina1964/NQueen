namespace NQueen.Kernel.Utilities;

public static class ProgressSettings
{
    public const double StartProgressValue = 0;

    public static int SolutionCountPerUpdate(byte boardSize) =>
        boardSize <= BoardSettings.SmallBoardSizeInUniqueSolutions
            ? 5
            : boardSize <= BoardSettings.MediumBoardSizeInUniqueSolutions
            ? 1_000 :
            100_000;
}
