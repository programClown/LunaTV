using System;
using System.Diagnostics;
using System.IO;
using LunaTV.Models;

namespace LunaTV.Constants;

public sealed class GlobalDefine
{
    private const int PlatformWindows = 1;
    private const int PlatformLinux = 2;
    private const int PlatformMac = 3;
    private static int s_platform;

    private static string? s_dataPath;
    private static string? s_downloadPath;
    private static string? s_screenshotPath;
    private static string? s_tempPath;
    private static string? s_logsPath;
    private static string? s_waveformsPath;
    private static string? s_spectrogramsPath;

    static GlobalDefine()
    {
        try
        {
            string fileName = OperatingSystem.IsWindows() ? "LunaTV.exe" : "LunaTV";
            FileVersionInfo app = FileVersionInfo.GetVersionInfo(Path.Combine(RootPath, fileName));
        }
        catch
        {
            // FileVersionInfo.GetVersionInfo may fail on platforms where the
            // executable is not present at RootPath (e.g. Android single-view).
        }
        EnsureDirectory(BootstrapDataPath);
    }

    /// <summary>
    ///     App版本
    /// </summary>
    public static string Version => typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    ///     App根地址
    /// </summary>
    public static string RootPath => AppDomain.CurrentDomain.BaseDirectory;

    public static string BootstrapDataPath =>
        Path.Combine(GetLocalAppDataPath(), "LunaTV");

    public static string DataPath => s_dataPath ?? BootstrapDataPath;

    public static string DownloadPath => s_downloadPath ?? DefaultDownloadPath;

    public static string ScreenshotPath => s_screenshotPath ?? Path.Combine(DefaultDownloadPath, "Screenshots");

    public static string TempPath => s_tempPath ?? Path.Combine(DataPath, "Temp");

    public static string LogsPath => s_logsPath ?? Path.Combine(DataPath, "Logs");

    /// <summary>
    ///     App数据库连接字符串
    /// </summary>
    public static string DbConn => Path.Combine(DataPath, "lunatv.sqlite");

    public static string AppJsonPath => Path.Combine(BootstrapDataPath, "lunatv-app.json");

    public static string? FFmpegPath { get; set; }

    public static string WaveformsFolder => s_waveformsPath ?? Path.Combine(DataPath, "Waveforms");
    public static int WaveformMinimumSampleRate { get; set; } = 126;
    public static string SpectrogramsFolder => s_spectrogramsPath ?? Path.Combine(DataPath, "Spectrograms");
    public static string SpectrogramStyle { get; set; } = SeSpectrogramStyle.Classic.ToString();

    public static bool UseFrameMode { get; set; } = false;

    private static string DefaultDownloadPath
    {
        get
        {
#if ANDROID
            // On Android, use the app's external files directory for downloads
            var extDir = global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
            return Path.Combine(extDir ?? GetLocalAppDataPath(), "LunaTV");
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LunaTV");
#endif
        }
    }

    public static StoragePathsConfig GetDefaultStoragePaths()
    {
        return new StoragePathsConfig
        {
            DataPath = BootstrapDataPath,
            DownloadPath = DefaultDownloadPath,
            ScreenshotPath = Path.Combine(DefaultDownloadPath, "Screenshots"),
            TempPath = Path.Combine(BootstrapDataPath, "Temp"),
            LogsPath = Path.Combine(BootstrapDataPath, "Logs"),
            WaveformsPath = Path.Combine(BootstrapDataPath, "Waveforms"),
            SpectrogramsPath = Path.Combine(BootstrapDataPath, "Spectrograms")
        };
    }

    public static StoragePathsConfig GetCurrentStoragePaths()
    {
        return new StoragePathsConfig
        {
            DataPath = DataPath,
            DownloadPath = DownloadPath,
            ScreenshotPath = ScreenshotPath,
            TempPath = TempPath,
            LogsPath = LogsPath,
            WaveformsPath = WaveformsFolder,
            SpectrogramsPath = SpectrogramsFolder
        };
    }

    public static void ApplyStoragePaths(StoragePathsConfig? storagePaths)
    {
        s_dataPath = NormalizeDirectoryPath(storagePaths?.DataPath);
        s_downloadPath = NormalizeDirectoryPath(storagePaths?.DownloadPath);
        s_screenshotPath = NormalizeDirectoryPath(storagePaths?.ScreenshotPath);
        s_tempPath = NormalizeDirectoryPath(storagePaths?.TempPath);
        s_logsPath = NormalizeDirectoryPath(storagePaths?.LogsPath);
        s_waveformsPath = NormalizeDirectoryPath(storagePaths?.WaveformsPath);
        s_spectrogramsPath = NormalizeDirectoryPath(storagePaths?.SpectrogramsPath);

        EnsureCoreDirectories();
    }

    public static void EnsureCoreDirectories()
    {
        EnsureDirectory(BootstrapDataPath);
        EnsureDirectory(DataPath);
        EnsureDirectory(DownloadPath);
        EnsureDirectory(ScreenshotPath);
        EnsureDirectory(TempPath);
        EnsureDirectory(LogsPath);
        EnsureDirectory(WaveformsFolder);
        EnsureDirectory(SpectrogramsFolder);
    }

    public static string? NormalizeDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var trimmed = path.Trim();
        try
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(trimmed));
        }
        catch
        {
            return trimmed;
        }
    }

    private static void EnsureDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        catch (Exception exception)
        {
            Trace.WriteLine($"创建目录失败：{path} {exception}");
        }
    }

    public static bool IsRunningOnWindows
    {
        get
        {
            if (s_platform == 0)
            {
                s_platform = GetPlatform();
            }

            return s_platform == PlatformWindows;
        }
    }

    public static bool IsRunningOnLinux
    {
        get
        {
            if (s_platform == 0)
            {
                s_platform = GetPlatform();
            }

            return s_platform == PlatformLinux;
        }
    }

    public static bool IsRunningOnMac
    {
        get
        {
            if (s_platform == 0)
            {
                s_platform = GetPlatform();
            }

            return s_platform == PlatformMac;
        }
    }

    private static int GetPlatform()
    {
        // Current versions of Mono report MacOSX platform as Unix
        return Environment.OSVersion.Platform == PlatformID.MacOSX ||
               (Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/Applications") &&
                Directory.Exists("/System") && Directory.Exists("/Users"))
            ? PlatformMac
            : Environment.OSVersion.Platform == PlatformID.Unix
                ? PlatformLinux
                : PlatformWindows;
    }

    private static string GetLocalAppDataPath()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(path)) return path;

        // Fallback for Android where GetFolderPath may return empty
        path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        if (!string.IsNullOrWhiteSpace(path)) return path;

#if ANDROID
        // Final fallback: use Android's internal files directory
        var context = global::Android.App.Application.Context;
        return context.FilesDir?.AbsolutePath ?? "/data/data/com.lunatv.app/files";
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
#endif
    }
}