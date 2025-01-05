namespace NQueen.GUI.Utils;

public static class Dimensions
{
    public static double ChessboardMinWidth => 140;

    public static double SolutionsListMaxHeight => 1050;

    public static double ChessboardMaxWidth => 1050;

    public static readonly GridLength ChessboardDefaultWidth = new(1000, GridUnitType.Pixel);

    public static double SolutionTextBoxMinHeight => 40;

    public static readonly GridLength SolutionsListDefaultHeight = new(1000, GridUnitType.Pixel);

    public static double GroupBoxMinHeight => 100;

    public static double GroupBoxMaxWidth => 350;

    public static double ProgressBarWidth => 250;

    public static double ProgressBarHeight => 25;

    public static double ProgressLabelHeight => 30;

    public static double ComboBoxHeight => 30;

    public static double SliderHeight => 25;

    public static readonly GridLength SolutionsListTitleHeight = new(30, GridUnitType.Pixel);
}
