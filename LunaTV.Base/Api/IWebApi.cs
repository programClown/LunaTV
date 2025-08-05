using Refit;

namespace LunaTV.Base.Api;

public interface IWebApi
{
    [Get("/j/search_tags")]
    Task<string> FetchDoubanTags(
        [Query] string type, //movie (电影) 或 tv(电视剧)
        CancellationToken cancellationToken = default);

    [Get("/j/search_subjects")]
    Task<string> FetchDoubanSubjectsByTag(
        [Query] string type, //movie (电影) 或 tv(电视剧)
        [Query] string tag,
        [Query] string? sort = null, //排序方式。常用值：recommend: 按综合推荐/热度排序；time: 按上映/播出时间排序 (最新)； rank: 按评分排序。
        [Query] int page_limit = 20,
        [Query] int page_start = 0,
        CancellationToken cancellationToken = default);

    /// 筛选接口
    [Get("/j/chart/top_list")]
    Task<string> GetchDoubanChartTopList(
        [Query] int type, //genreId, e.g., "剧情", "喜剧"
        [Query] string interval_id = "100:90",
        [Query] string action = "",
        [Query] int start = 0,
        [Query] int limit = 100,
        CancellationToken cancellationToken = default);

    /// 新版筛选接口
    [Get("/j/new_search_subjects")]
    Task<string> NewApiGetchDoubanChartTopList(
        [Query] string? tags = null, //主要用于指定大的内容分类，如 "电影", "电视剧", "动画", "综艺", "纪录片", "短片"。如果为空，则可能代表所有类型。
        [Query] string? genres = null, //指定题材/类型，可为单个值或多个值以逗号分隔 (URL编码，如 "科幻,喜剧")。例如："科幻", "喜剧", "动作"
        [Query] string? countries = null, //指定制片国家/地区，可为单个值或多个值以逗号分隔 (URL编码)。例如："中国大陆", "美国", "日本"。
        [Query] string? sort = null, //排序方式。- `T`: 按热度/综合推荐排序;- `R`: 按时间排序 (最新);- `S`: 按评分排序;
        [Query] int start = 0,
        [Query] string range = "0,10",
        CancellationToken cancellationToken = default);

    /// 筛选接口
    [Get("/j/subject_suggest")]
    Task<string> GetchDoubanSearchSuggestions(
        [Query] string q, //如红楼梦
        CancellationToken cancellationToken = default);
}