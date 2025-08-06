using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LunaTV.Models;

public class DoubanSubjectsResponse
{
    public List<DoubanSubject> Subjects { get; set; }
}

public class DoubanSubject
{
    [JsonPropertyName("episodes_info")] public string EpisodesInfo { get; set; }

    [JsonPropertyName("rate")] public string Rate { get; set; }

    [JsonPropertyName("cover_x")] public int CoverX { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; }

    [JsonPropertyName("playable")] public bool Playable { get; set; }

    [JsonPropertyName("cover")] public string Cover { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("cover_y")] public int CoverY { get; set; }

    [JsonPropertyName("is_new")] public bool IsNew { get; set; }
}