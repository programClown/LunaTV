using SqlSugar;

namespace LunaTV.Base.Models;

[SugarTable("search_history")]
public class SearchHistory
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }

    public string MovieName { get; set; }
    public DateTime CreateTime { get; set; }
}