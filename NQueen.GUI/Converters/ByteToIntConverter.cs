namespace NQueen.GUI.Converters;

public class ByteToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is byte byteValue ? (int)byteValue : (object)0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int intValue ? (byte)intValue : (object)0;
    }
}