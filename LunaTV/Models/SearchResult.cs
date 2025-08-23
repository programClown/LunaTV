namespace LunaTV.Models;

public class SearchResult
{
    public string Id { get; set; } //vod_id
    public string Source { get; set; } = string.Empty; //网站源
    public string SourceName { get; set; } = string.Empty; //网站源名称
    public string Name { get; set; } = string.Empty; //电影名称
    public string Tag { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Cover { get; set; } = string.Empty;
    public string Descriptor { get; set; } = string.Empty;
    public string ReMark { get; set; } = string.Empty;
    public string ApiUrlAttr { get; set; } = string.Empty;
}