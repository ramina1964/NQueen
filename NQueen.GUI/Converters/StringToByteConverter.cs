namespace NQueen.GUI.Converters;

public class StringToByteConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && byte.TryParse(str, out byte result))
        {
            return result;
        }

        // Return a default value or handle the invalid input case
        return DependencyProperty.UnsetValue;
    }
}
