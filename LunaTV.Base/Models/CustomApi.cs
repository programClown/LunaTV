using SqlSugar;

namespace LunaTV.Base.Models;

[SugarTable("custom_api")]
public class CustomApi
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    public string? Name { set; get; }
    public string? Url { set; get; }
    public string? Detail { set; get; }
    public bool IsAdult { set; get; }
    public bool IsEnable { set; get; } = true;
    public DateTime CreateTime { get; set; }
}