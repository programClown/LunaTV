using SqlSugar;

namespace LunaTV.Base.Models;

[SugarTable("media_download")]
public class MediaDownload
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    [SugarColumn(IsNullable = true)] public string? Source { get; set; } //来源
    [SugarColumn(IsNullable = true)] public string? Name { get; set; } //电影名
    [SugarColumn(IsNullable = true)] public string? Episode { get; set; } //剧集
    [SugarColumn(IsNullable = true)] public string? Url { get; set; } //播放地址
    [SugarColumn(IsNullable = true)] public string? LocalPath { get; set; } // 本地地址
    public bool IsDownloaded { get; set; } // 是否下载完成
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}