namespace NQueen.GUI.Converters;

public class NullImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Treat null or empty string as unset
        if (value is null || (value is string str && string.IsNullOrWhiteSpace(str)))
            return DependencyProperty.UnsetValue;

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
