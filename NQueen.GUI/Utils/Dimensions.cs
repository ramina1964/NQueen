namespace NQueen.GUI.Utils;

public static class Dimensions
{
    public static double MinWidth => 140;

    public static double MaxChessboardWidth => 1050;

    public static readonly GridLength DefaultChessboardWidth = new(1000, GridUnitType.Pixel);

    public static double MinHeight => 40;

    public static readonly GridLength DefaultChessboardHeight = new(1000, GridUnitType.Pixel);

    public static double MaxChessboardHeight => 1050;

    public static double GroupBoxMinHeight => 100;

    public static double GroupBoxMaxWidth => 350;

    public static double ProgressBarWidth => 250;

    public static double ProgressBarHeight => 25;

    public static double ProgressLabelHeight => 30;

    public static double ComboBoxHeight => 30;

    public static double SliderHeight => 25;

    public static readonly GridLength SolutionListTitleHeight = new(30, GridUnitType.Pixel);
}
