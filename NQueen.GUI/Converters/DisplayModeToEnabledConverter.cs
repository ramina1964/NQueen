namespace NQueen.GUI.Converters;

public class DisplayModeToEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DisplayMode displayMode &&
            parameter is string targetMode &&
            Enum.TryParse<DisplayMode>(targetMode, out var parsed))
        {
            return displayMode == parsed;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
