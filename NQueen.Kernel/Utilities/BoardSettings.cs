namespace NQueen.Kernel.Utilities;

public static class BoardSettings
{
    public const int MaxBoardSizeInSingleSolution = 37;
    public const int MaxBoardSizeInUniqueSolutions = 17;
    public const int MaxBoardSizeInAllSolutions = 17;

    public const int SmallBoardSizeInUniqueSolutions = 10;
    public const int MediumBoardSizeInUniqueSolutions = 15;

    public const byte ByteMaxValue = byte.MaxValue;
    public const int DefaultBoardSize = 8;
    public const int MinBoardSize = 1;
    public const int MaxBoardSize = MaxBoardSizeInSingleSolution;
}
