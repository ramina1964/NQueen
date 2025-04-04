namespace NQueen.GUI.Utils;

public static class Dimensions
{
    // Selected Solution: Spanning over all three columns of the first row
    public static double SelectedSolutionMinHeight => 40;

    // Solutions List: The first column of the second row
    public static double SolutionsListMaxHeight => 1050;

    public static readonly GridLength SolutionsListDefaultHeight = new(1, GridUnitType.Star);

    public static readonly GridLength SolutionsListTitleHeight = new(30, GridUnitType.Pixel);

    // Chessboard: The second column of the second row
    public static double ChessboardMaxWidth => 1050;

    public static readonly GridLength ChessboardDefaultWidth = new(1, GridUnitType.Star);

    // User Panels: The third row of the second row, consisting of Input, Output and Simulation Panels
    public static double UserPanelsMinWidth => 120;

    public static double GroupBoxMinHeight => 100;

    public static double GroupBoxMaxWidth => 350;

    public static double ProgressBarWidth => 250;

    public static double ProgressBarHeight => 25;

    public static double ProgressLabelHeight => 30;

    public static double ComboBoxHeight => 30;

    public static double SliderHeight => 25;

    // Dynamic sizes for chessboard squares
    public static double ChessboardSquareSize(double availableWidth, double availableHeight, int boardSize)
    {
        return Math.Min(availableWidth, availableHeight) / boardSize;
    }
}
