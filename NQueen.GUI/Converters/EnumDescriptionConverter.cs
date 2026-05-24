namespace NQueen.GUI.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum en)
            return DependencyProperty.UnsetValue;

        return GetDescription(en);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Enum.ToObject(targetType, value);

    public static string GetDescription(Enum en)
    {
        var member = en.GetType().GetMember(en.ToString()).FirstOrDefault();
        var description = member?.GetCustomAttribute<DescriptionAttribute>()?.Description;
        return description ?? en.ToString();
    }
}
