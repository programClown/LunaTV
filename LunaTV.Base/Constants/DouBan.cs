namespace LunaTV.Base.Constants;

// 豆瓣排行榜 API 类型 ID 映射
public record DouBanChartGenreIds(string Genre, int Id)
{
    public static List<DouBanChartGenreIds> AllIds = new()
    {
        new("剧情", 11),
        new("喜剧", 24),
        new("动作", 5),
        new("爱情", 13),
        new("科幻", 17),
        new("动画", 25),
        new("悬疑", 10),
        new("惊悚", 19),
        new("恐怖", 20),
        new("纪录片", 1),
        new("短片", 23),
        new("情色", 6),
        new("同性", 26),
        new("音乐", 14),
        new("歌舞", 7),
        new("家庭", 28),
        new("儿童", 8),
        new("传记", 2),
        new("历史", 4),
        new("战争", 22),
        // IDs 9 和 21 似乎不存在
    };

    public int GetIdByGenre(string genre)
    {
        var select = AllIds.FirstOrDefault(a => a.Genre == genre);

        return select?.Id ?? -1;
    }
}