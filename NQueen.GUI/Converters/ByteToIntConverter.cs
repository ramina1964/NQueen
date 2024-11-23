namespace NQueen.GUI.Converters;

public class ByteToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte byteValue)
        {
            return (int)byteValue;
        }

        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return (byte)intValue;
        }

        return 0;
    }
}