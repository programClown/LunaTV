using SqlSugar;

namespace LunaTV.Base.Models;

[SugarTable("api_source")]
public class ApiSource
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    [SugarColumn(IsNullable = true)] public string? Source { set; get; }
    [SugarColumn(IsNullable = true)] public string? Name { set; get; }
    [SugarColumn(IsNullable = true)] public string? ApiBaseUrl { set; get; }
    [SugarColumn(IsNullable = true)] public string? DetailBaseUrl { set; get; }
    public bool IsAdult { set; get; }
    public bool IsCustomApi { set; get; }
    public bool IsEnable { set; get; } = true;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}