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
            if (byte.TryParse(input, out byte result))
            {
                return result;
            }
            else if (int.TryParse(input, out int intResult))
            {
                if (intResult < byte.MinValue || intResult > byte.MaxValue)
                {
                    SetErrorMessage("Board size must be between 1 and 255.");
                }
                else
                {
                    SetErrorMessage("Please enter a valid number between 1 and 255.");
                }
                return DependencyProperty.UnsetValue;
            }
            else
            {
                SetErrorMessage("Please enter a valid number between 1 and 255.");
                return DependencyProperty.UnsetValue;
            }
        }
        return DependencyProperty.UnsetValue;
    }

    private void SetErrorMessage(string message)
    {
        if (Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.InputViewModel.ErrorMessage = message;
            mainViewModel.InputViewModel.IsErrorVisible = true;
        }
    }
}
