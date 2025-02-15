namespace NQueen.GUI.Converters;

public class DivideByBoardSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is double actualSize && parameter is int boardSize && boardSize > 0 ? actualSize / boardSize : (object)0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

