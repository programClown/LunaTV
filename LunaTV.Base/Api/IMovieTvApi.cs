using Refit;

namespace LunaTV.Base.Api;

public interface IMovieTvApi
{
    /// 搜索视频
    /// wd = 搜索关键词，etc.电影名
    [Get("/api.php/provide/vod/?ac=videolist")]
    Task<string> SearchVideos([Query] string wd, CancellationToken cancellationToken = default);

    /// 分页搜索视频
    /// wd = 搜索关键词，etc.电影名
    /// pg = 页码
    [Get("/api.php/provide/vod/?ac=videolist")]
    Task<string> PageSearchVideos([Query] string wd, [Query] int pg,
        CancellationToken cancellationToken = default);

    /// 获取视频详情
    /// ids = /^[\w-]+$/
    /// 匹配字母、数字、下划线和短横线
    /// 对应vod_id
    [Get("/api.php/provide/vod/?ac=videolist")]
    Task<string> GetVideoDetail([Query] string ids, CancellationToken cancellationToken = default);

    /// id = /^[\w-]+$/ 
    /// 匹配字母、数字、下划线和短横线
    /// 对应vod_id
    /// _t = 时间戳，清理缓存
    [Get("/index.php/vod/detail/id/{id}.html")]
    Task<string> GetSpecialSourceVideoDetail(string id, CancellationToken cancellationToken = default);
}