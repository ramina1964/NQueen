namespace NQueen.GUI.Converters;

public class BoardSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string input)
        {
            //  Check if 'input' is a valid integer
            if (int.TryParse(input, out int intResult))
            {
                // Check if 'intResult' is within the valid range
                if (intResult >= BoardSettings.MinBoardSize && intResult <= BoardSettings.ByteMaxValue)
                {
                    return intResult;
                }

                // Here is intResult out of range.
                SetErrorMessage(Messages.SizeOutOfRangeError);
                return DependencyProperty.UnsetValue;
            }

            // Here is input an invalid integer.
            SetErrorMessage(Messages.SizeFormatError);
            return DependencyProperty.UnsetValue;
        }

        return DependencyProperty.UnsetValue;
    }

    private static void SetErrorMessage(string message)
    {
        if (Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.InputViewModel.ErrorMessage = message;
            mainViewModel.InputViewModel.IsErrorVisible = true;
        }
    }
}
