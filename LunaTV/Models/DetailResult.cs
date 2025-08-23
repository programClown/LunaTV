using System.Collections.Generic;
using LunaTV.Base.Constants;

namespace LunaTV.Models;

public class EpisodeSubject
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public class DetailResult
{
    public List<EpisodeSubject>? Episodes { get; set; } = new();
    public string? DetailUrl { get; set; }

    // video info
    public string? VodId { set; get; }
    public string? Title { get; set; } //vod_name
    public string? Cover { get; set; } //vod pic
    public string? Desc { get; set; } //vod_content
    public string? Type { get; set; } //type_name
    public string? Year { get; set; } //vod_year
    public string? Area { get; set; } //vod_area
    public string? Director { get; set; } //vod_director
    public string? Actor { get; set; } //vod_actor
    public string? Remark { get; set; } //vod_remark


    //添加信息
    /// 对应<see cref="ApiSourceInfo.ApiSitesConfig"/>的key
    public string? Source { get; set; }

    /// Source是自定义的复制“自定义源”，对应<see cref="ApiSourceInfo.ApiSitesConfig"/>的value.Name
    public string? SourceName { get; set; }
}