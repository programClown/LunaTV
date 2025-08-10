namespace LunaTV.Models;

public class PlayConfig
{
    public string Autoplay { get; set; } = "true";
    public string AllowFullscreen { get; set; } = "true";
    public string Width { get; set; } = "100%";
    public string Height { get; set; } = "600";
    public int Timeout { get; set; } = 15000; // 播放器加载超时时间
    public string FilterAds { get; set; } = "true"; // 是否启用广告过滤
    public string AutoPlayNext { get; set; } = "true"; // 默认启用自动连播功能
    public string AdFilteringEnabled { get; set; } = "true"; // 默认开启分片广告过滤
    public string AdFilteringStorage { get; set; } = "adFilteringEnabled"; // 存储广告过滤设置的键名
}