﻿namespace NQueen.GUI.Converters;

public class DivideByBoardSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualSize && parameter is int boardSize && boardSize > 0)
        {
            return actualSize / boardSize;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

