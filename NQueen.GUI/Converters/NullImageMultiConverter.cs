namespace NQueen.GUI.Converters;

public class NullImageMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var imagePath = values[0] as string;
        var defaultImagePath = values[1] as string;

        return string.IsNullOrEmpty(imagePath) ? defaultImagePath : imagePath;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
