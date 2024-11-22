namespace NQueen.GUI.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            bool booleanValue => booleanValue
                ? Visibility.Visible
                : Visibility.Collapsed,
            _ => (object)Visibility.Collapsed,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            Visibility visibility => visibility == Visibility.Visible,
            _ => (object)false,
        };
}
