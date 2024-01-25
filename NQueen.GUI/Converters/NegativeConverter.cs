namespace NQueen.GUI.Converters;

public class NegativeConverter : MarkupExtension, IValueConverter
{
    public NegativeConverter() : base() { }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ReturnNegative(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        ReturnNegative(value);

    private static object ReturnNegative(object value)
    {
        object result = null;
        Dictionary<Type, Action> @switch = new()
        {
            { typeof(bool), () => result = !(bool)value },
            { typeof(byte), () => result = -1 * (byte)value },
            { typeof(short), () => result = -1 * (short)value },
            { typeof(int), () => result = -1 * (int)value },
            { typeof(long), () => result = -1 * (long)value },
            { typeof(float), () => result = -1f * (float)value },
            { typeof(double), () => result = -1d * (double)value },
            { typeof(decimal), () => result = -1m * (decimal)value }
        };

        @switch[value.GetType()]();
        
        return
            result
            ?? throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        _converter ??= new NegativeConverter();

        return _converter;
    }

    private static NegativeConverter _converter = null;
}
