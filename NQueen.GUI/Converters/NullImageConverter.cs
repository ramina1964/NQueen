namespace NQueen.GUI.Converters;

public class NullImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var stringValue = value as string;
        return string.IsNullOrEmpty(stringValue) ? parameter as string : stringValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
