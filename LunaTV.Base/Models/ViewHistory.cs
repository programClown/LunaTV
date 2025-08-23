using SqlSugar;

namespace LunaTV.Base.Models;

[SugarTable("view_history")]
public class ViewHistory
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    [SugarColumn(IsNullable = true)] public string? VodId { get; set; } //电影Id
    [SugarColumn(IsNullable = true)] public string? Name { get; set; } //电影名
    [SugarColumn(IsNullable = true)] public string? Episode { get; set; } //剧集
    [SugarColumn(IsNullable = true)] public string? Url { get; set; } //播放地址
    [SugarColumn(IsNullable = true)] public string? Source { get; set; } //来源
    public int PlaybackPosition { get; set; } //播放位置
    public int Duration { get; set; } //总时长
    public int TotalEpisodeCount { get; set; } //总集数
    public bool IsLocal { get; set; } //本地影视
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}