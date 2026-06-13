namespace LunaTV.Models;

public class AppJsonConfig
{
    public string Version { get; set; } = "1.0.0";
    public Player Player { get; set; } = new();
    public StoragePathsConfig StoragePaths { get; set; } = new();
}

public class Player
{
    public float Vol { get; set; } = 50; //音量
    public bool Muted { get; set; } //
}

public class StoragePathsConfig
{
    public const string DataPathPropertyName = nameof(DataPath);
    public const string DownloadPathPropertyName = nameof(DownloadPath);
    public const string ScreenshotPathPropertyName = nameof(ScreenshotPath);
    public const string TempPathPropertyName = nameof(TempPath);
    public const string LogsPathPropertyName = nameof(LogsPath);
    public const string WaveformsPathPropertyName = nameof(WaveformsPath);
    public const string SpectrogramsPathPropertyName = nameof(SpectrogramsPath);

    public string? DataPath { get; set; }
    public string? DownloadPath { get; set; }
    public string? ScreenshotPath { get; set; }
    public string? TempPath { get; set; }
    public string? LogsPath { get; set; }
    public string? WaveformsPath { get; set; }
    public string? SpectrogramsPath { get; set; }
}