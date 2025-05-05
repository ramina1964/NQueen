namespace NQueen.GUI.Utils;

// Todo: Find out if this class is useful in deciding the chessboard dimensions in ChessboardViewModel.
public static class LayoutUtils
{
    /// <summary>
    /// Calculates the available dimension for a square area based on the width and height.
    /// </summary>
    /// <param name="element">The UI element to measure.</param>
    /// <returns>The smaller of the element's actual width and height.</returns>
    public static double CalculateAvailableDimension(FrameworkElement element)
    {
        return element == null
            ? throw new ArgumentNullException(nameof(element))
            : Math.Min(element.ActualWidth, element.ActualHeight);
    }
}
