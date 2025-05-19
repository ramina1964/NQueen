namespace NQueen.GUI.Converters;

public class FirstValidationErrorConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var errors = value as ReadOnlyObservableCollection<ValidationError>;
        return errors != null && errors.Count > 0
            ? errors[0].ErrorContent
            : null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture) => throw new NotImplementedException();
}
