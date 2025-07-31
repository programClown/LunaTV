using FreeSql.DataAnnotations;

namespace LunaTV.Base.Models;

public class SearchHistory
{
    [Column(IsIdentity = true, IsPrimary = true)]
    public int Id { get; set; }

    public string MovieName { get; set; }
    public DateTime CreateTime { get; set; }
}