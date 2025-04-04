namespace NQueen.GUI.Converters;

// Todo: Use ValueConverter from CommunityToolkit.Mvvm, instead
public class StringToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string input)
        {
            if (int.TryParse(input, out int intResult))
            {
                if (intResult >= BoardSettings.MinBoardSize && intResult <= BoardSettings.MaxBoardSize)
                    return intResult;

                SetErrorMessage(Messages.SizeOutOfRangeError);
                return DependencyProperty.UnsetValue;
            }

            SetErrorMessage(Messages.SizeOutOfRangeError);
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
