using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LunaTV.Converters;

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            var topLevel = App.TopLevel;
            return status switch
            {
                "未开始" => topLevel?.TryFindResource("LabelTagSolidGreyBackground", out var valueg) == true
                    ? (Brush)valueg!
                    : Brush.Parse("#888D92"), // 灰色
                "下载中" => topLevel?.TryFindResource("LabelTagSolidOrangeBackground", out var valuey) == true
                    ? (Brush)valuey!
                    : Brush.Parse("#FFAE43"), // 黄色
                "已完成" => topLevel?.TryFindResource("LabelTagSolidGreenBackground", out var valueg2) == true
                    ? (Brush)valueg2!
                    : Brush.Parse("#97C65F"), // 绿色
                "下载失败" => topLevel?.TryFindResource("LabelTagSolidRedBackground", out var valueg3) == true
                    ? (Brush)valueg3!
                    : Brush.Parse("#FC725A"), // 红色
                _ => Brush.Parse("#888D92") // 默认灰色
            };
        }

        return Brush.Parse("#888D92"); // 默认灰色
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return "";
    }
}