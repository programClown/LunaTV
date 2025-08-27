using SqlSugar;

namespace LunaTV.Base.Models;

[SugarTable("player_config")]
public class PlayerConfig
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    public bool Autoplay { set; get; }
    public bool AllowFullscreen { set; get; }
    public int Timeout { set; get; } = 15000; // 播放器加载超时时间
    public bool FilterAds { set; get; } = true; // 是否启用广告过滤
    public bool AutoPlayNext { set; get; } = true; // 默认启用自动连播功能
    public bool AdFilteringEnabled { set; get; } = true; // 默认开启分片广告过滤
    public bool DoubanApiEnabled { set; get; } // 豆瓣API
    public bool HomeAutoLoadDoubanEnabled { set; get; } // 首页自动加载豆瓣数据
    public bool ForceApiNeedSpecialSource { set; get; } //强制使用api
}