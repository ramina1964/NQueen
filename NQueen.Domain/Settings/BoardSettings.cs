namespace NQueen.Domain.Settings;

public static class BoardSettings
{
    public const int DefaultBoardSize = 8;
    public const int MinSize = 1;

    // Maximum board size supported by the bitmask solver implementation
    // (uses 64-bit masks). Th number 64 is the practical upper bound with a single ulong;
    // beyond that a multi-word bitset would be required.
    public const int MaxBitmaskBoardSize = 64;

    public const int MaxSizeForSingle = 37;
    public const int MaxSizeForUnique = 25;
    public const int MaxSizeForAll = 25;

    public const char WhiteQueenChar = '\u2655';
    public const string QueenImageResource =
        "pack://application:,,,/NQueen.GUI;component/Images/WhiteQueen.png";
}
