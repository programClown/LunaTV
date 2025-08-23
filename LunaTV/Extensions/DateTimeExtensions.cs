using System;

namespace LunaTV.Extensions;

public static class DateTimeExtensions
{
    public static string ToFriendlyTime(this DateTime dateTime)
    {
        TimeSpan timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "刚刚";

        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}分钟前";

        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}小时前";

        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays}天前";

        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)}个月前";

        return $"{(int)(timeSpan.TotalDays / 365)}年前";
    }
}