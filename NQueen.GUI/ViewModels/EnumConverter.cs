﻿namespace NQueen.GUI.ViewModels;

public class EnumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? DependencyProperty.UnsetValue : GetDescription((Enum)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
    {
        return Enum.ToObject(targetType, value);
    }

    public static string GetDescription(Enum en)
    {
        Type type = en.GetType();
        var memInfo = type.GetMember(en.ToString());
        if (memInfo != null && memInfo.Length > 0)
        {
            object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }

        return en.ToString();
    }
}
