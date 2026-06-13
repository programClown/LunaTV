using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace LunaTV.Base.DB;

public static class SqlSugarServiceExtensions
{
    private static bool s_sqliteInitialized;
    public static string? LastInitError { get; private set; }

    public static IServiceCollection AddSqlSugarClient(this IServiceCollection services, string dbPath)
    {
        // Ensure SQLitePCL native provider is initialized (required on Android)
        if (!s_sqliteInitialized)
        {
            try
            {
                SQLitePCL.Batteries_V2.Init();
                s_sqliteInitialized = true;
            }
            catch { /* already initialized or not available */ }
        }

        // Ensure the database directory exists
        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(dbDir) && !Directory.Exists(dbDir))
        {
            try { Directory.CreateDirectory(dbDir); }
            catch { /* best effort */ }
        }

        var db = new SqlSugarClient(new ConnectionConfig()
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"Data Source={dbPath};",
            IsAutoCloseConnection = true, // 自动释放连接
            InitKeyType = InitKeyType.Attribute, // 主键配置方式
            MoreSettings = new ConnMoreSettings()
            {
                SqliteCodeFirstEnableDescription = true, //启用备注
                IsNoReadXmlDescription = true // Android doesn't have XML doc files
            }
        });
        db.Aop.OnLogExecuted = (sql, pars) => { Console.WriteLine(sql); };

        // 创建数据库表 — always attempt to ensure tables exist
        try
        {
            db.CodeFirst.SetStringDefaultLength(200).InitTables(GetDbTypes());
        }
        catch (Exception e)
        {
            // Store error for later retrieval since Console doesn't reach logcat
            LastInitError = e.ToString();
            // Try individual tables as fallback
            foreach (var type in GetDbTypes())
            {
                try { db.CodeFirst.SetStringDefaultLength(200).InitTables(type); }
                catch (Exception ex) { LastInitError += $"\n\n[{type.Name}]: {ex.Message}"; }
            }
        }

        // Column migrations — wrapped to prevent crash on Android
        try
        {
        if (!db.DbMaintenance.IsAnyColumn("player_config", "DoubanApiEnabled"))
        {
            db.DbMaintenance.AddColumn("player_config", new DbColumnInfo()
            {
                DbColumnName = "DoubanApiEnabled",
                TableName = "player_config",
                DataType = "bit",
                IsNullable = false,
                DefaultValue = "0"
            });
        }

        if (!db.DbMaintenance.IsAnyColumn("player_config", "HomeAutoLoadDoubanEnabled"))
        {
            db.DbMaintenance.AddColumn("player_config", new DbColumnInfo()
            {
                DbColumnName = "HomeAutoLoadDoubanEnabled",
                TableName = "player_config",
                DataType = "bit",
                IsNullable = false,
                DefaultValue = "0"
            });
        }

        if (!db.DbMaintenance.IsAnyColumn("player_config", "ForceApiNeedSpecialSource"))
        {
            db.DbMaintenance.AddColumn("player_config", new DbColumnInfo()
            {
                DbColumnName = "ForceApiNeedSpecialSource",
                TableName = "player_config",
                DataType = "bit",
                IsNullable = false,
                DefaultValue = "0"
            });
        }

        if (!db.DbMaintenance.IsAnyColumn("player_config", "DoubanMovieTags"))
        {
            db.DbMaintenance.AddColumn("player_config", new DbColumnInfo()
            {
                DbColumnName = "DoubanMovieTags",
                TableName = "player_config",
                DataType = "varchar(2000)",
                IsNullable = true
            });
        }

        if (!db.DbMaintenance.IsAnyColumn("player_config", "DoubanTvTags"))
        {
            db.DbMaintenance.AddColumn("player_config", new DbColumnInfo()
            {
                DbColumnName = "DoubanTvTags",
                TableName = "player_config",
                DataType = "varchar(2000)",
                IsNullable = true
            });
        }

        if (!db.DbMaintenance.IsAnyColumn("media_download", "DownloadStatus"))
            AddMediaDownloadColumn("DownloadStatus", "int", false, "0");
        if (!db.DbMaintenance.IsAnyColumn("media_download", "Progress"))
            AddMediaDownloadColumn("Progress", "real", false, "0");
        if (!db.DbMaintenance.IsAnyColumn("media_download", "DownloadedBytes"))
            AddMediaDownloadColumn("DownloadedBytes", "bigint", false, "0");
        if (!db.DbMaintenance.IsAnyColumn("media_download", "TotalBytes"))
            AddMediaDownloadColumn("TotalBytes", "bigint", false, "0");
        if (!db.DbMaintenance.IsAnyColumn("media_download", "SizeText"))
            AddMediaDownloadColumn("SizeText", "varchar(2000)", true);
        if (!db.DbMaintenance.IsAnyColumn("media_download", "SpeedText"))
            AddMediaDownloadColumn("SpeedText", "varchar(2000)", true);
        if (!db.DbMaintenance.IsAnyColumn("media_download", "RemainingTimeText"))
            AddMediaDownloadColumn("RemainingTimeText", "varchar(2000)", true);
        if (!db.DbMaintenance.IsAnyColumn("media_download", "ErrorMessage"))
            AddMediaDownloadColumn("ErrorMessage", "varchar(2000)", true);
        if (!db.DbMaintenance.IsAnyColumn("media_download", "OutputFilePath"))
            AddMediaDownloadColumn("OutputFilePath", "varchar(2000)", true);
        if (!db.DbMaintenance.IsAnyColumn("media_download", "Cover"))
            AddMediaDownloadColumn("Cover", "varchar(2000)", true);
        if (!db.DbMaintenance.IsAnyColumn("view_history", "Cover"))
        {
            db.DbMaintenance.AddColumn("view_history", new DbColumnInfo
            {
                DbColumnName = "Cover",
                TableName = "view_history",
                DataType = "varchar(2000)",
                IsNullable = true
            });
        }

        }
        catch (Exception e)
        {
            Console.WriteLine($"[LunaTV] DB migration error: {e}");
        }

        services.AddSingleton<ISqlSugarClient>(db);
        return services;

        void AddMediaDownloadColumn(string name, string dataType, bool isNullable, string? defaultValue = null)
        {
            db.DbMaintenance.AddColumn("media_download", new DbColumnInfo
            {
                DbColumnName = name,
                TableName = "media_download",
                DataType = dataType,
                IsNullable = isNullable,
                DefaultValue = defaultValue
            });
        }
    }

    public static void AddSugarRepository(this IServiceCollection services)
    {
        services.AddScoped<SugarRepository<SearchHistory>>();
        services.AddScoped<SugarRepository<ApiSource>>();
        services.AddScoped<SugarRepository<ViewHistory>>();
        services.AddScoped<SugarRepository<PlayerConfig>>();
        services.AddScoped<SugarRepository<MediaDownload>>();
    }

    public static Type[] GetDbTypes()
    {
        return
        [
            typeof(SearchHistory), typeof(ApiSource), typeof(ViewHistory), typeof(PlayerConfig), typeof(MediaDownload)
        ];
    }
}