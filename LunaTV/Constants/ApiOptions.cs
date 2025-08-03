namespace LunaTV.Constants;

public enum ApiType
{
    Json,
    Html
}

public record ApiEndpoint
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string DetailBaseUrl { get; init; } = string.Empty;
    public ApiType Type { get; init; } = ApiType.Json;
    public string SearchPath { get; init; } = string.Empty;
    public string DetailPath { get; init; } = string.Empty;
}

public record ApiOptions
{
    public ApiEndpoint DouBan { get; init; } = new()
    {
        Name = "电影天堂资源",
        Url = "https://www.douban.com",
        DetailBaseUrl = "https://www.douban.com",
        SearchPath = "/search",
        DetailPath = "/subject/{id}/"
    };
}