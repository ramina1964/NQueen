namespace NQueen.GUI.Converters;

// Todo: Use ValueConverter from CommunityToolkit.Mvvm, instead
public class NegativeConverter : MarkupExtension, IValueConverter
{
    public NegativeConverter() : base() { }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ReturnNegative(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        ReturnNegative(value);

    private static object ReturnNegative(object value) =>
        value switch
        {
            bool b => !b,
            byte b => -b,
            short s => -s,
            int i => -i,
            long l => -l,
            float f => -f,
            double d => -d,
            decimal m => -m,
            _ => throw new NotImplementedException()
        };

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _converter ??= new NegativeConverter();

    private static NegativeConverter _converter = null;
}
