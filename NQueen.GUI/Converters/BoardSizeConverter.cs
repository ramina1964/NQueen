namespace NQueen.GUI.Converters;

public class BoardSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string input)
        {
            switch (input)
            {
                case string _ when int.TryParse(input, out int byteResult):
                    return byteResult;

                case string _ when int.TryParse(input, out int intResult):
                    if (intResult < byte.MinValue || intResult > byte.MaxValue)
                    {
                        SetErrorMessage(Messages.SizeOutOfRangeError);
                    }
                    return DependencyProperty.UnsetValue;

                default:
                    SetErrorMessage(Messages.SizeFormatError);
                    return DependencyProperty.UnsetValue;
            }
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
