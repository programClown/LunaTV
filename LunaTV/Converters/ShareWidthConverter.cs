using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SqlSugar.Extensions;

namespace LunaTV.Converters;

public class ShareWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && parameter is string minWidthStr)
        {
            double minWidth = double.Parse(minWidthStr);
            if (width < minWidth)
            {
                return minWidth;
            }

            int percentage = (int)(width / (minWidth + 8));
            return (width - 8 - 8 * percentage) / percentage;
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}