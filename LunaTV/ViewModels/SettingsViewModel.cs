using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Dialogs;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.Services;
using LunaTV.ViewModels.Base;
using Semi.Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppJsonConfigService _appJsonConfigService;
    private AppJsonConfig _appJsonConfig;
    private bool _isLoadingStoragePaths;

    [ObservableProperty]
    private string? _currentAppTheme = "Auto";

    [ObservableProperty]
    private FlowDirection _currentFlowDirection;

    [ObservableProperty] private string? _dataPath;
    [ObservableProperty] private string? _downloadPath;
    [ObservableProperty] private string? _screenshotPath;
    [ObservableProperty] private string? _tempPath;
    [ObservableProperty] private string? _logsPath;
    [ObservableProperty] private string? _waveformsPath;
    [ObservableProperty] private string? _spectrogramsPath;
    [ObservableProperty] private string? _storagePathHint;

    public SettingsViewModel(AppJsonConfigService appJsonConfigService)
    {
        _appJsonConfigService = appJsonConfigService;
        _appJsonConfig = _appJsonConfigService.ReadJson<AppJsonConfig>() ?? new AppJsonConfig();
        EnsureConfigDefaults();
        LoadStoragePathsFromGlobal();
    }

    public string CurrentVersion => typeof(App).Assembly.GetName().Version?.ToString();
    public string CurrentAvaloniaVersion => typeof(Application).Assembly.GetName().Version?.ToString();

    public List<string> AppThemes => ["Auto", "Light", "Dark", "Aquatic", "Desert", "Dusk", "NightSky"];

    public FlowDirection[] AppFlowDirections { get; } =
        new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft };

    partial void OnCurrentAppThemeChanged(string? value)
    {
        Application? app = App.Current;
        if (app is null) return;
        ThemeVariant theme = value switch
        {
            "Auto" => app.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "Aquatic" => SemiTheme.Aquatic,
            "Desert" => SemiTheme.Desert,
            "Dusk" => SemiTheme.Dusk,
            "NightSky" => SemiTheme.NightSky,
            _ => app.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark
        };
        app.RequestedThemeVariant = theme;

        App.Notification?.Show(
            new Notification("主题已更新", $"当前主题是{value}"),
            NotificationType.Success,
            classes: ["Light"]);
    }

    partial void OnCurrentFlowDirectionChanged(FlowDirection value)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime cdl)
        {
            if (cdl.MainWindow.FlowDirection == value)
            {
                return;
            }
            cdl.MainWindow.FlowDirection = value;
        }
    }

    partial void OnDataPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.DataPath), value);
    partial void OnDownloadPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.DownloadPath), value);
    partial void OnScreenshotPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.ScreenshotPath), value);
    partial void OnTempPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.TempPath), value);
    partial void OnLogsPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.LogsPath), value);
    partial void OnWaveformsPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.WaveformsPath), value);
    partial void OnSpectrogramsPathChanged(string? value) => SaveStoragePath(nameof(StoragePathsConfig.SpectrogramsPath), value);

    [RelayCommand]
    private async Task BrowseStoragePath(string? pathKey)
    {
        if (string.IsNullOrWhiteSpace(pathKey)) return;
        if (App.StorageProvider is null || !App.StorageProvider.CanPickFolder)
        {
            App.Notification?.Show(new Notification("无法选择目录", "当前环境不支持目录选择"), NotificationType.Warning);
            return;
        }

        var folders = await App.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"选择{GetStoragePathTitle(pathKey)}",
            AllowMultiple = false
        });

        var folder = folders.Count > 0 ? folders[0].Path.LocalPath : null;
        if (string.IsNullOrWhiteSpace(folder)) return;

        await ApplySelectedStoragePath(pathKey, folder);
    }

    [RelayCommand]
    private void ResetStoragePath(string? pathKey)
    {
        if (string.IsNullOrWhiteSpace(pathKey)) return;
        AssignStoragePath(_appJsonConfig.StoragePaths, pathKey, null);
        SaveApplyAndReloadStoragePaths();
        App.Notification?.Show(new Notification("存储位置已恢复默认", GetStoragePathTitle(pathKey)), NotificationType.Success);
    }

    [RelayCommand]
    private void ResetAllStoragePaths()
    {
        _appJsonConfig.StoragePaths = new StoragePathsConfig();
        SaveApplyAndReloadStoragePaths();
        App.Notification?.Show(new Notification("存储位置已恢复默认", "所有路径已恢复为默认值"), NotificationType.Success);
    }

    private async Task ApplySelectedStoragePath(string pathKey, string selectedPath)
    {
        var normalizedPath = GlobalDefine.NormalizeDirectoryPath(selectedPath);
        if (string.IsNullOrWhiteSpace(normalizedPath)) return;

        if (pathKey == nameof(StoragePathsConfig.DataPath) && !IsSamePath(GlobalDefine.DataPath, normalizedPath))
        {
            var result = await MessageBox.ShowAsync(
                "数据目录会影响数据库、豆瓣图片缓存等。完整生效需要重启应用。\n\n是否先复制当前数据目录内容到新目录？",
                "数据目录变更",
                MessageBoxIcon.Warning,
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes && !CopyCurrentDataPathTo(normalizedPath))
            {
                return;
            }
        }

        AssignStoragePath(_appJsonConfig.StoragePaths, pathKey, normalizedPath);
        SaveApplyAndReloadStoragePaths();

        var message = pathKey == nameof(StoragePathsConfig.DataPath)
            ? "已保存，数据库位置将在重启后完全生效"
            : normalizedPath;
        App.Notification?.Show(new Notification($"{GetStoragePathTitle(pathKey)}已更新", message), NotificationType.Success);
    }

    private bool CopyCurrentDataPathTo(string newDataPath)
    {
        var oldDataPath = GlobalDefine.DataPath;
        if (IsSamePath(oldDataPath, newDataPath)) return true;

        try
        {
            if (IsSubdirectoryOf(newDataPath, oldDataPath))
            {
                App.Notification?.Show(new Notification("复制数据目录失败", "新目录不能位于当前数据目录内部"), NotificationType.Error);
                return false;
            }

            CopyDirectory(oldDataPath, newDataPath);
            return true;
        }
        catch (Exception ex)
        {
            App.Notification?.Show(new Notification("复制数据目录失败", ex.Message), NotificationType.Error);
            return false;
        }
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;

        Directory.CreateDirectory(targetDirectory);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, directory));
            Directory.CreateDirectory(target);
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, file));
            if (File.Exists(target)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private void SaveStoragePath(string pathKey, string? value)
    {
        if (_isLoadingStoragePaths) return;
        AssignStoragePath(_appJsonConfig.StoragePaths, pathKey, GlobalDefine.NormalizeDirectoryPath(value));
        SaveAndApplyStoragePaths();
    }

    private void SaveApplyAndReloadStoragePaths()
    {
        SaveAndApplyStoragePaths();
        LoadStoragePathsFromGlobal();
    }

    private void SaveAndApplyStoragePaths()
    {
        EnsureConfigDefaults();
        _appJsonConfigService.WriteJson(_appJsonConfig);
        GlobalDefine.ApplyStoragePaths(_appJsonConfig.StoragePaths);
        StoragePathHint = "存储位置已保存。数据目录变更后需重启应用才能切换数据库文件。";
    }

    private void LoadStoragePathsFromGlobal()
    {
        _isLoadingStoragePaths = true;
        var paths = GlobalDefine.GetCurrentStoragePaths();
        DataPath = paths.DataPath;
        DownloadPath = paths.DownloadPath;
        ScreenshotPath = paths.ScreenshotPath;
        TempPath = paths.TempPath;
        LogsPath = paths.LogsPath;
        WaveformsPath = paths.WaveformsPath;
        SpectrogramsPath = paths.SpectrogramsPath;
        StoragePathHint = "数据目录影响数据库位置，修改后需要重启应用完全生效。";
        _isLoadingStoragePaths = false;
    }

    private void EnsureConfigDefaults()
    {
        _appJsonConfig.Player ??= new Player();
        _appJsonConfig.StoragePaths ??= new StoragePathsConfig();
    }

    private static void AssignStoragePath(StoragePathsConfig storagePaths, string pathKey, string? value)
    {
        switch (pathKey)
        {
            case nameof(StoragePathsConfig.DataPath):
                storagePaths.DataPath = value;
                break;
            case nameof(StoragePathsConfig.DownloadPath):
                storagePaths.DownloadPath = value;
                break;
            case nameof(StoragePathsConfig.ScreenshotPath):
                storagePaths.ScreenshotPath = value;
                break;
            case nameof(StoragePathsConfig.TempPath):
                storagePaths.TempPath = value;
                break;
            case nameof(StoragePathsConfig.LogsPath):
                storagePaths.LogsPath = value;
                break;
            case nameof(StoragePathsConfig.WaveformsPath):
                storagePaths.WaveformsPath = value;
                break;
            case nameof(StoragePathsConfig.SpectrogramsPath):
                storagePaths.SpectrogramsPath = value;
                break;
        }
    }

    private static string GetStoragePathTitle(string pathKey)
    {
        return pathKey switch
        {
            nameof(StoragePathsConfig.DataPath) => "数据目录",
            nameof(StoragePathsConfig.DownloadPath) => "下载目录",
            nameof(StoragePathsConfig.ScreenshotPath) => "截图目录",
            nameof(StoragePathsConfig.TempPath) => "下载临时目录",
            nameof(StoragePathsConfig.LogsPath) => "日志目录",
            nameof(StoragePathsConfig.WaveformsPath) => "波形缓存目录",
            nameof(StoragePathsConfig.SpectrogramsPath) => "频谱缓存目录",
            _ => "存储目录"
        };
    }

    private static bool IsSamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        return string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static bool IsSubdirectoryOf(string candidate, string parent)
    {
        var candidatePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate)) + Path.DirectorySeparatorChar;
        var parentPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar;
        return candidatePath.StartsWith(parentPath,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}