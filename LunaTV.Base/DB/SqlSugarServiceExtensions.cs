using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace LunaTV.Base.DB;

public static class SqlSugarServiceExtensions
{
    public static IServiceCollection AddSqlSugarClient(this IServiceCollection services, string dbPath)
    {
        var db = new SqlSugarClient(new ConnectionConfig()
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"Data Source={dbPath};",
            IsAutoCloseConnection = true, // 自动释放连接 
            InitKeyType = InitKeyType.Attribute, // 主键配置方式
            MoreSettings = new ConnMoreSettings()
            {
                SqliteCodeFirstEnableDescription = true //启用备注
            }
        });
        db.Aop.OnLogExecuted = (sql, pars) => { Console.WriteLine(sql); };

        // 创建数据库表
        if (!File.Exists(dbPath) || db.DbMaintenance.GetTableInfoList(false).Count != GetDbTypes().Length)
        {
            try
            {
                db.CodeFirst.SetStringDefaultLength(200).InitTables(GetDbTypes());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        services.AddSingleton<ISqlSugarClient>(db);
        return services;
    }

    public static void AddSugarRepository(this IServiceCollection services)
    {
        services.AddScoped<SugarRepository<SearchHistory>>();
        services.AddScoped<SugarRepository<ApiSource>>();
        services.AddScoped<SugarRepository<ViewHistory>>();
        services.AddScoped<SugarRepository<PlayerConfig>>();
    }

    public static Type[] GetDbTypes()
    {
        return
        [
            typeof(SearchHistory), typeof(ApiSource), typeof(ViewHistory), typeof(PlayerConfig)
        ];
    }
}