using System;
using System.IO;

namespace LunaTV.Constants;

public sealed class GlobalDefine
{
    static GlobalDefine()
    {
        var fileName = OperatingSystem.IsWindows() ? "LunaTV.exe" : "LunaTV";
        var app = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(RootPath, fileName));
    }

    /// <summary>
    /// App版本
    /// </summary>
    public static string Version => typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// App根地址
    /// </summary>
    public static string RootPath => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// App数据库连接字符串
    /// </summary>
    public static string DbConn => Path.Combine(RootPath, "lunatv.sqlite");
}