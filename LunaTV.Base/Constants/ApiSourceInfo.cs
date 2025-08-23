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