using SqlSugar;

namespace LunaTV.Base.DB.UnitOfWork;

/// <summary>
/// 通过(DbContext/GetMyRepository)获取的自定义仓储类
/// </summary>
/// <typeparam name="T"></typeparam>
public class SugarDbSet<T> : SimpleClient<T> where T : class, new()
{
    // You can add custom methods here if needed
}