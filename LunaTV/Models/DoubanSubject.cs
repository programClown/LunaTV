using System.Collections.Generic;
using System.Text.Json.Serialization;
using LunaTV.Converters;

namespace LunaTV.Models;

public class DoubanSubjectsResponse
{
    public List<DoubanSubject> Subjects { get; set; } = [];
}

public class DoubanSubject
{
    [JsonPropertyName("episodes_info")] public string? EpisodesInfo { get; set; }

    [JsonPropertyName("rate")] public string? Rate { get; set; }

    [JsonConverter(typeof(FlexibleIntConverter))]
    [JsonPropertyName("cover_x")]
    public int CoverX { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleBoolConverter))]
    [JsonPropertyName("playable")]
    public bool Playable { get; set; }

    [JsonPropertyName("cover")] public string Cover { get; set; } = string.Empty;

    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleIntConverter))]
    [JsonPropertyName("cover_y")]
    public int CoverY { get; set; }

    [JsonConverter(typeof(FlexibleBoolConverter))]
    [JsonPropertyName("is_new")]
    public bool IsNew { get; set; }

    [JsonPropertyName("year")] public string? Year { get; set; }
}
