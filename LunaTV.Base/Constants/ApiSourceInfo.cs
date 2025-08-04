using System.Text.Json.Nodes;

namespace LunaTV.Base.Constants;

public enum ApiType
{
    Json,
    Html, // For sources where details are scraped from HTML
}

public record ApiSourceInfo(
    string ApiBaseUrl,
    string Name,
    string? DetailBaseUrl, // For HTML detail pages or different detail API base
    ApiType ApiType, // To distinguish between JSON API and HTML scraping for details
    string? SearchPath, // Specific search path if different from default
    string? DetailPath // Specific detail path if different from default (for JSON APIs)
    // Example: some sources might use /vodsearch instead of /api.php/provide/vod/...
)
{
    // Note: Detail paths for HTML sources are usually part of detail_base_url construction
    // Search paths and detail paths for JSON sources can use defaults or be overridden here
    public static Dictionary<string, ApiSourceInfo> ApiSitesConfig = new()
    {
        {
            "dyttzy",
            new ApiSourceInfo(
                "http://caiji.dyttzyapi.com",
                "电影天堂资源",
                "http://caiji.dyttzyapi.com",
                ApiType.Json,
                null,
                null)
        },
        {
            "ruyi",
            new ApiSourceInfo(
                "https://cj.rycjapi.com",
                "如意资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "bfzy",
            new ApiSourceInfo(
                "https://bfzyapi.com",
                "暴风资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "tyyszy",
            new ApiSourceInfo(
                "https://tyyszy.com",
                "天涯资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "xiaomaomi",
            new ApiSourceInfo(
                "https://zy.xiaomaomi.cc",
                "小猫咪资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "ffzy",
            new ApiSourceInfo(
                "http://ffzy5.tv",
                "非凡影视",
                "http://ffzy5.tv",
                ApiType.Html,
                null,
                "/index.php/vod/detail/id/{id}.html")
        },
        {
            "heimuer",
            new ApiSourceInfo(
                "https://json.heimuer.xyz",
                "黑木耳",
                "https://heimuer.tv",
                ApiType.Html,
                null,
                "/index.php/vod/detail/id/{id}.html")
        },
        {
            "zy360",
            new ApiSourceInfo(
                "https://360zy.com",
                "360资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "wolong",
            new ApiSourceInfo(
                "https://wolongzyw.com",
                "卧龙资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "hwba",
            new ApiSourceInfo(
                "https://cjhwba.com",
                "华为吧资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "jisu",
            new ApiSourceInfo(
                "https://jszyapi.com",
                "极速资源",
                "https://jszyapi.com",
                ApiType.Json,
                null,
                null)
        },
        {
            "dbzy",
            new ApiSourceInfo(
                "https://dbzy.com",
                "豆瓣资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "mozhua",
            new ApiSourceInfo(
                "https://mozhuazy.com",
                "魔爪资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "mdzy",
            new ApiSourceInfo(
                "https://www.mdzyapi.com",
                "魔都资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "zuid",
            new ApiSourceInfo(
                "https://api.zuidapi.com",
                "最大资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "yinghua",
            new ApiSourceInfo(
                "https://m3u8.apiyhzy.com",
                "樱花资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "baidu",
            new ApiSourceInfo(
                "https://api.apibdzy.com",
                "百度云资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "wujin",
            new ApiSourceInfo(
                "https://api.wujinapi.me",
                "无尽资源",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "wwzy",
            new ApiSourceInfo(
                "https://wwzy.tv",
                "旺旺短剧",
                null,
                ApiType.Json,
                null,
                null)
        },
        {
            "ikun",
            new ApiSourceInfo(
                "https://ikunzyapi.com",
                "iKun资源",
                null,
                ApiType.Json,
                null,
                null)
        },
    };
}

public record struct ApiPathConfig(string Search)
{
    public static ApiPathConfig Default = new ApiPathConfig("/api.php/provide/vod/?ac=videolist&wd=");
}

public record struct SearchResponse(UInt16 Code, string? Msg, JsonArray? List);