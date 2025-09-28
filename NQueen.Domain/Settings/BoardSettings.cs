namespace NQueen.Domain.Settings;

public static class BoardSettings
{
    public const int DefaultBoardSize = 8;
    public const int RelativeFactor = 8;
    public const int MinSize = 1;

    public const int ExtraSmallSizeForUniqueMode = 6;
    public const int SmallSizeForUniqueMode = 10;
    public const int MediumSizeForUniqueMode = 13;
    public const int RelativeLargeSizeForUniqueMode = 15;

    // Maximum board size supported by the bitmask solver implementation
    // (uses 64-bit masks). Th number 64 is the practical upper bound with a single ulong;
    // beyond that a multi-word bitset would be required.
    public const int MaxBitmaskBoardSize = 64;

    public const int MaxSizeForSingle = 37;
    public const int MaxSizeForUnique = 18;
    public const int MaxSizeForAll = 18;

    public const string DefaultQueenImagePath = @"..\..\Images\WhiteQueen.png";
    public const char WhiteQueenChar = '\u2655';
    public const string QueenImageResource =
        "pack://application:,,,/NQueen.GUI;component/Images/WhiteQueen.png";
}
