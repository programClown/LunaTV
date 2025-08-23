using System;
using System.IO;

namespace LunaTV.Constants;

public sealed class GlobalDefine
{
    static GlobalDefine()
    {
        var fileName = OperatingSystem.IsWindows() ? "LunaTV.exe" : "LunaTV";
        var app = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(RootPath, fileName));

        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appPath = Path.Combine(basePath, "LunaTV");
        if (!Directory.Exists(appPath))
            Directory.CreateDirectory(appPath);
    }

    /// <summary>
    /// App版本
    /// </summary>
    public static string Version => typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// App根地址
    /// </summary>
    public static string RootPath => AppDomain.CurrentDomain.BaseDirectory;

    public static string DataPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LunaTV");

    /// <summary>
    /// App数据库连接字符串
    /// </summary>
    public static string DbConn => Path.Combine(DataPath, "lunatv.sqlite");
}