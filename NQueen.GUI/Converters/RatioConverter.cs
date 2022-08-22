namespace NQueen.GUI.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class RatioConverter : MarkupExtension, IValueConverter
{
    public RatioConverter() { }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Do not let the culture default to local to prevent variable outcome to decimal syntax
        double size = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        return size.ToString("G0", CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Read only converter...
        throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return _instance ?? (_instance = new RatioConverter());
    }

    private static RatioConverter _instance;
}
