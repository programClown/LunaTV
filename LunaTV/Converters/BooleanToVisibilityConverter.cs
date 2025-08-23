using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LunaTV.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Return true (Visible) if true, false (Hidden) if false
            return boolValue;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool visibility)
        {
            return visibility;
        }

        return false;
    }
}