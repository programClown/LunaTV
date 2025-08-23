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

// "episode" : "50",
// "img" : "https://img9.doubanio.com/view/photo/s_ratio_poster/public/p491154576.jpg",
// "title" : "红楼梦",
// "url" : "https://movie.douban.com/subject/3014183/?suggest=%E7%BA%A2%E6%A5%BC%E6%A2%A6",
// "type" : "movie",
// "year" : "2010",
// "sub_title" : "红楼梦",
// "id" : "3014183"
public class DoubanSuggestionSubject
{
    [JsonPropertyName("episode")] public string Episode { get; set; }

    [JsonPropertyName("img")] public string Img { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("year")] public string Year { get; set; }

    [JsonPropertyName("sub_title")] public string SubTitle { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }
}
