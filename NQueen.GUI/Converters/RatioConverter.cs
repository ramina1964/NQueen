﻿namespace NQueen.GUI.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class RatioConverter : MarkupExtension, IValueConverter
{
    public RatioConverter() { }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Do not let the culture default to local to prevent variable outcome to decimal syntax
        var size = System.Convert.ToDouble(value) * System.Convert
            .ToDouble(parameter, CultureInfo.InvariantCulture);

        return size.ToString("G0", CultureInfo.InvariantCulture);
    }

    // Read only converter...
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return _instance ??= new RatioConverter();
    }

    private static RatioConverter _instance;
}
