namespace NQueen.GUI.Utils;

// Todo: Use dimensions defined here to set the size of the GUI elements
public static class Dimensions
{
    // Selected Solution: Spanning over all three columns of the first row
    public static double SelectedSolutionMinHeight => 40;

    // Solutions List: The first column of the second row
    public static double SolutionsListMaxHeight => 1050;

    public static readonly GridLength SolutionsListDefaultHeight = new(1000, GridUnitType.Pixel);

    public static readonly GridLength SolutionsListTitleHeight = new(30, GridUnitType.Pixel);

    // Chessboard: The second column of the second row
    public static double ChessboardMaxWidth => 1050;

    public static readonly GridLength ChessboardDefaultWidth = new(1000, GridUnitType.Pixel);

    // User Panels: The third row of the second row, consisting of Input, Ouput and Simulation Panels
    public static double UserPanelsMinWidth => 120;

    public static double GroupBoxMinHeight => 100;

    public static double GroupBoxMaxWidth => 350;

    public static double ProgressBarWidth => 250;

    public static double ProgressBarHeight => 25;

    public static double ProgressLabelHeight => 30;

    public static double ComboBoxHeight => 30;

    public static double SliderHeight => 25;
}
