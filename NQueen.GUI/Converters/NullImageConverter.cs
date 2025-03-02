namespace NQueen.GUI.Converters;

public class NullImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var imagePath = value as string;
        return string.IsNullOrEmpty(imagePath) ? parameter as string : imagePath;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
