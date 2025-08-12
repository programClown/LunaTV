using System.Text.Json.Nodes;

namespace LunaTV.Base.Constants;

public record ApiSourceInfo(
    string ApiBaseUrl,
    string Name,
    string? DetailBaseUrl,
    bool IsCustomApi = false,
    bool IsAdult = false
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
                "http://caiji.dyttzyapi.com")
        },
        {
            "ruyi",
            new ApiSourceInfo(
                "https://cj.rycjapi.com",
                "如意资源",
                null)
        },
        {
            "bfzy",
            new ApiSourceInfo(
                "https://bfzyapi.com",
                "暴风资源",
                null)
        },
        {
            "tyyszy",
            new ApiSourceInfo(
                "https://tyyszy.com",
                "天涯资源",
                null)
        },
        {
            "xiaomaomi",
            new ApiSourceInfo(
                "https://zy.xiaomaomi.cc",
                "小猫咪资源",
                null)
        },
        {
            "ffzy",
            new ApiSourceInfo(
                "http://ffzy5.tv",
                "非凡影视",
                "http://ffzy5.tv")
        },
        {
            "heimuer",
            new ApiSourceInfo(
                "https://json.heimuer.xyz",
                "黑木耳",
                "https://heimuer.tv")
        },
        {
            "zy360",
            new ApiSourceInfo(
                "https://360zy.com",
                "360资源",
                null)
        },
        {
            "iqiyi",
            new ApiSourceInfo(
                "https://www.iqiyizyapi.com",
                "爱奇艺",
                null)
        },
        {
            "wolong",
            new ApiSourceInfo(
                "https://wolongzyw.com",
                "卧龙资源",
                null)
        },
        {
            "hwba",
            new ApiSourceInfo(
                "https://cjhwba.com",
                "华为吧资源",
                null)
        },
        {
            "jisu",
            new ApiSourceInfo(
                "https://jszyapi.com",
                "极速资源",
                "https://jszyapi.com")
        },
        {
            "dbzy",
            new ApiSourceInfo(
                "https://dbzy.com",
                "豆瓣资源",
                null)
        },
        {
            "mozhua",
            new ApiSourceInfo(
                "https://mozhuazy.com",
                "魔爪资源",
                null)
        },
        {
            "mdzy",
            new ApiSourceInfo(
                "https://www.mdzyapi.com",
                "魔都资源",
                null)
        },
        {
            "zuid",
            new ApiSourceInfo(
                "https://api.zuidapi.com",
                "最大资源",
                null)
        },
        {
            "yinghua",
            new ApiSourceInfo(
                "https://m3u8.apiyhzy.com",
                "樱花资源",
                null)
        },
        {
            "baidu",
            new ApiSourceInfo(
                "https://api.apibdzy.com",
                "百度云资源",
                null)
        },
        {
            "wujin",
            new ApiSourceInfo(
                "https://api.wujinapi.me",
                "无尽资源",
                null)
        },
        {
            "wwzy",
            new ApiSourceInfo(
                "https://wwzy.tv",
                "旺旺短剧",
                null)
        },
        {
            "ikun",
            new ApiSourceInfo(
                "https://ikunzyapi.com",
                "iKun资源",
                null)
        },
        {
            "lzi",
            new ApiSourceInfo(
                "https://cj.lziapi.com",
                "量子资源站",
                null)
        },
    };
}

public record UserAgent(string Value)
{
    public static string GetRandomUserAgent()
    {
        List<UserAgent> userAgents =
        [
            new(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"),
            new(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15"),
            new("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/121.0")
        ];

        return userAgents[new Random().Next(userAgents.Count)].Value;
    }
}