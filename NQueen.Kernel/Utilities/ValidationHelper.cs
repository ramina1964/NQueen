namespace NQueen.Kernel.Utilities;

public static class ValidationHelper
{
    public static bool IsBoardSizeFormattedCorrectly(string boardSize) =>
        int.TryParse(boardSize, out int result) &&
            result >= BoardSettings.MinBoardSize && result <= BoardSettings.IntMaxValue;
}