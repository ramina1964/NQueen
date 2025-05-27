namespace NQueen.GUI.Utils;

public static class LayoutUtils
{
    public static double CalculateAvailableDimension(FrameworkElement element)
    {
        return element == null
            ? throw new ArgumentNullException(nameof(element))
            : Math.Min(element.ActualWidth, element.ActualHeight);
    }
}
