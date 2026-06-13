using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using M3U8Download;
using Microsoft.Extensions.DependencyInjection;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVDownloadViewModel : ViewModelBase
{
    private readonly Dictionary<int, DownloadManager> _downloadManagers = new();

    private readonly DispatcherTimer _downloadTimer;
    private readonly SugarRepository<MediaDownload> _mediaDownloadTable;
    private readonly Dictionary<int, int> _downloadPersistTicks = new();
    private MediaDownloadViewModel? _currentDownloadingMVM;
    private int _lastLoggedWaitingCount = -1;
    private string? _lastLoggedDownloadingName;

    [ObservableProperty] private int _downloadingCount;
    [ObservableProperty] private string _downloadName = "曼达洛人";
    [ObservableProperty] private bool _downloadOrHistoryChecked = true;
    [ObservableProperty] private string _downloadUrl = "https://vod.360zyx.vip/20250708/7T2xjBRd/index.m3u8";
    private bool _isInitialized;

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _waitingCount;

    public TVDownloadViewModel()
    {
        _mediaDownloadTable = App.Services.GetRequiredService<SugarRepository<MediaDownload>>();
        MediaDownloadViewModels = new ObservableCollection<MediaDownloadViewModel>();
        FilteredDownloadViewModels = new ObservableCollection<MediaDownloadViewModel>();
        MediaHistoryViewModels = new ObservableCollection<MediaDownloadViewModel>();
        _downloadTimer = new DispatcherTimer
            (TimeSpan.FromSeconds(1), DispatcherPriority.Background, DownloadTimerOnTick);
        _downloadTimer.Start();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await LoadDownloadTasksFromDBAsync();
            await LoadDownloadHistoryFromDbAsync();
        });
    }

    public ObservableCollection<MediaDownloadViewModel> MediaDownloadViewModels { get; set; }
    public ObservableCollection<MediaDownloadViewModel> FilteredDownloadViewModels { get; set; }
    [ObservableProperty] private string? _downloadFilterText;
    public ObservableCollection<MediaDownloadViewModel> MediaHistoryViewModels { get; set; }

    partial void OnDownloadFilterTextChanged(string? value)
    {
        RefreshFilteredDownloads();
    }

    private void RefreshFilteredDownloads()
    {
        var filterText = DownloadFilterText?.Trim();
        var filteredDownloads = string.IsNullOrWhiteSpace(filterText)
            ? MediaDownloadViewModels
            : new ObservableCollection<MediaDownloadViewModel>(MediaDownloadViewModels.Where(download => MatchesDownloadFilter(download, filterText)));

        FilteredDownloadViewModels.Clear();
        foreach (var download in filteredDownloads)
        {
            FilteredDownloadViewModels.Add(download);
        }
    }

    private static bool MatchesDownloadFilter(MediaDownloadViewModel download, string filterText)
    {
        return ContainsIgnoreCase(download.Name, filterText)
               || ContainsIgnoreCase(download.Episode, filterText)
               || ContainsIgnoreCase(download.Source, filterText)
               || ContainsIgnoreCase(download.OutputFilePath, filterText)
               || ContainsIgnoreCase(download.Url, filterText);
    }

    private static bool ContainsIgnoreCase(string? value, string filterText)
    {
        return value?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true;
    }

    // 从数据库加载下载任务
    private async Task LoadDownloadTasksFromDBAsync()
    {
        var downloads = await _mediaDownloadTable.GetListAsync(x => true);
        foreach (var downloadTask in downloads)
        {
            if (!MediaDownloadViewModels.Any(x => x.Url == downloadTask.Url))
            {
                var downloadStatus = (DownloadType)downloadTask.DownloadStatus;
                if (downloadStatus == DownloadType.Downloading) downloadStatus = DownloadType.None;

                var mediaDownloadViewModel = new MediaDownloadViewModel
                {
                    Id = downloadTask.Id,
                    Source = downloadTask.Source,
                    Name = downloadTask.Name,
                    Episode = downloadTask.Episode,
                    Url = downloadTask.Url,
                    DownloadStatus = downloadStatus,
                    LocalPath = downloadTask.LocalPath,
                    Status = ToStatusWord(downloadStatus),
                    Progress = downloadTask.Progress,
                    SizeStr = downloadTask.SizeText ?? "--/--",
                    Speed = downloadTask.SpeedText ?? "0 Bps",
                    RemainingTime = downloadTask.RemainingTimeText ?? "--:--:--",
                    ErrorMessage = downloadTask.ErrorMessage,
                    DownloadedBytes = downloadTask.DownloadedBytes,
                    TotalBytes = downloadTask.TotalBytes,
                    OutputFilePath = downloadTask.OutputFilePath,
                    Cover = downloadTask.Cover,
                    CreateTime = downloadTask.CreateTime,
                    UpdateTime = downloadTask.UpdateTime
                };
                var resolvedOutputFilePath = DownloadFileResolver.ResolveExistingFile(
                    mediaDownloadViewModel.OutputFilePath,
                    mediaDownloadViewModel.LocalPath,
                    mediaDownloadViewModel.Name,
                    mediaDownloadViewModel.Episode);
                if (downloadStatus == DownloadType.Downloaded && !string.IsNullOrWhiteSpace(resolvedOutputFilePath))
                {
                    mediaDownloadViewModel.OutputFilePath = resolvedOutputFilePath;
                    if (downloadTask.OutputFilePath != resolvedOutputFilePath)
                    {
                        downloadTask.OutputFilePath = resolvedOutputFilePath;
                        await _mediaDownloadTable.UpdateAsync(downloadTask);
                    }
                }

                MediaDownloadViewModels.Add(mediaDownloadViewModel);
                AddDownloadCounts(downloadStatus);
            }
        }

        RefreshFilteredDownloads();
        _isInitialized = true;
#if !ANDROID
        if (!File.Exists(GlobalDefine.FFmpegPath))
        {
            App.Notification?.Show(new Notification("错误", "FFmpeg路径配置错误", NotificationType.Error),
                NotificationType.Error);
        }
#endif
    }

    private async Task LoadDownloadHistoryFromDbAsync()
    {
        var downloads = await _mediaDownloadTable.GetListAsync(x => x.IsDownloaded);
        MediaHistoryViewModels.Clear();
        foreach (var download in downloads)
        {
            MediaHistoryViewModels.Add(new MediaDownloadViewModel
            {
                Id = download.Id,
                Name = download.Name,
                Episode = download.Episode,
                Url = download.Url,
                DownloadStatus = DownloadType.None,
                LocalPath = download.LocalPath,
                Status = string.IsNullOrEmpty(DownloadFileResolver.GetMediaPath(download.LocalPath, download.Name))
                    ? StatusWord.DownloadFailed
                    : StatusWord.Downloaded,
                Progress = 100,
                UpdateTime = download.UpdateTime
            });
        }
    }

    private async void DownloadTimerOnTick(object? sender, EventArgs e)
    {
#if ANDROID
        if (!_isInitialized) return;
#else
        if (!_isInitialized || !File.Exists(GlobalDefine.FFmpegPath))
        {
            return;
        }
#endif

        try
        {
            if (_currentDownloadingMVM is null)
            {
                if (MediaDownloadViewModels.Count > 0)
                {
                    if (_lastLoggedWaitingCount != MediaDownloadViewModels.Count)
                    {
                        _lastLoggedWaitingCount = MediaDownloadViewModels.Count;
                        Trace.WriteLine($"等待下载：{MediaDownloadViewModels.Count}");
                    }

                    _currentDownloadingMVM =
                        MediaDownloadViewModels.FirstOrDefault(x => x.DownloadStatus == DownloadType.None);
                    if (_currentDownloadingMVM != null)
                    {
                        Console.WriteLine($"开始下载：{_currentDownloadingMVM.Name}");
                        PreDownload(_currentDownloadingMVM);
                    }
                }
            }
            else
            {
                if (_currentDownloadingMVM.DownloadStatus == DownloadType.Downloading)
                {
                    if (_lastLoggedDownloadingName != _currentDownloadingMVM.Name)
                    {
                        _lastLoggedDownloadingName = _currentDownloadingMVM.Name;
                        Trace.WriteLine($"下载中：{_currentDownloadingMVM.Name}");
                    }

                    // 刷新下载进度
                    Downloading(_currentDownloadingMVM);
                    if (_currentDownloadingMVM.DownloadStatus != DownloadType.Downloading)
                    {
                        Trace.WriteLine($"下载完成：{_currentDownloadingMVM.Name}");
                        await PersistDownloadAsync(_currentDownloadingMVM);
                        RefreshFilteredDownloads();
                        _downloadManagers.Remove(_currentDownloadingMVM.Id);
                        _downloadPersistTicks.Remove(_currentDownloadingMVM.Id);
                        _lastLoggedWaitingCount = -1;
                        _lastLoggedDownloadingName = null;
                        _currentDownloadingMVM =
                            MediaDownloadViewModels.FirstOrDefault(x => x.DownloadStatus == DownloadType.None);
                        if (_currentDownloadingMVM != null)
                        {
                            PreDownload(_currentDownloadingMVM);
                        }
                    }
                }
                else
                {
                    _currentDownloadingMVM = null;
                }
            }
        }
        catch (Exception exception)
        {
            Trace.WriteLine(exception);
            if (_currentDownloadingMVM is not null)
            {
                _currentDownloadingMVM.DownloadStatus = DownloadType.DownloadFailed;
                _currentDownloadingMVM.Status = StatusWord.DownloadFailed;
                _currentDownloadingMVM.ErrorMessage = exception.Message;
                await PersistDownloadAsync(_currentDownloadingMVM);
                RefreshFilteredDownloads();
            }
        }
    }

    ~TVDownloadViewModel()
    {
        _downloadTimer.Stop();
    }

    public async Task AddMediaDownload(string name, string url, string folder = "", string source = "", string? cover = null)
    {
        if (MediaDownloadViewModels.Any(x => x.Url == url))
        {
            App.Notification?.Show(new Notification("警告", $"【{name} {url}】\n 已在下载队列", NotificationType.Warning),
                NotificationType.Warning);
            return;
        }

        if (!File.Exists(GlobalDefine.FFmpegPath))
        {
            App.Notification?.Show(new Notification("错误", "FFmpeg路径配置错误", NotificationType.Error),
                NotificationType.Error);
            return;
        }

        var id = 0;
        // 检查是否已存在
        var mediaDownload = await _mediaDownloadTable.GetSingleAsync(md => md.Url == url);
        if (mediaDownload != null)
        {
            var result =
                await MessageBox.ShowAsync($"是否重新下载 【{name}】 ?", "提示", MessageBoxIcon.Warning, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            id = mediaDownload.Id;
        }

        // 添加到数据库
        var md = new MediaDownload
        {
            Id = id,
            Source = source,
            Name = name,
            Episode = string.Empty,
            Url = url,
            IsDownloaded = false,
            DownloadStatus = (int)DownloadType.None,
            Progress = 0,
            DownloadedBytes = 0,
            TotalBytes = 0,
            SizeText = "--/--",
            SpeedText = "0 Bps",
            RemainingTimeText = "--:--:--",
            ErrorMessage = null,
            OutputFilePath = null,
            Cover = cover,
            LocalPath = Path.Combine(GlobalDefine.DownloadPath, folder),
            CreateTime = id > 0 ? mediaDownload?.CreateTime ?? DateTime.Now : DateTime.Now,
            UpdateTime = DateTime.Now
        };
        if (id > 0)
        {
            await _mediaDownloadTable.UpdateAsync(md);
        }

        else
        {
            id = await _mediaDownloadTable.Context.Insertable(md).ExecuteReturnIdentityAsync();
        }

        // 添加到列表
        MediaDownloadViewModels.Add(new MediaDownloadViewModel
        {
            Id = id,
            Source = source,
            Name = name,
            Episode = string.Empty,
            Url = url,
            DownloadStatus = DownloadType.None,
            LocalPath = Path.Combine(GlobalDefine.DownloadPath, folder),
            Status = StatusWord.Unstarted,
            Progress = 0,
            SizeStr = "--/--",
            Speed = "0 Bps",
            RemainingTime = "--:--:--",
            ErrorMessage = null,
            DownloadedBytes = 0,
            TotalBytes = 0,
            OutputFilePath = null,
            Cover = cover,
            CreateTime = md.CreateTime,
            UpdateTime = DateTime.Now
        });

        AddDownloadCounts(DownloadType.None);
        RefreshFilteredDownloads();
    }

    private async void PreDownload(MediaDownloadViewModel mdvm)
    {
        mdvm.DownloadStatus = DownloadType.Downloading;
        var downloadManager = new DownloadManager();
        if (downloadManager.Option is not null)
        {
            downloadManager.Option.TmpDir = GlobalDefine.TempPath;
            downloadManager.Option.LogFilePath = Path.Combine(GlobalDefine.LogsPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{mdvm.Id}.log");
            downloadManager.SetFFmpegPath(GlobalDefine.FFmpegPath);
        }

        _downloadManagers[mdvm.Id] = downloadManager;
        mdvm.Status = StatusWord.Downloading;
        mdvm.ErrorMessage = null;
        await PersistDownloadAsync(mdvm);
        _ = Task.Run(async () =>
        {
            try
            {
                await _downloadManagers[mdvm.Id].DownloadAsync(mdvm.Url!, mdvm.LocalPath!, mdvm.Name!);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (mdvm.DownloadStatus == DownloadType.Downloading)
                    {
                        mdvm.DownloadStatus = DownloadType.DownloadFailed;
                        mdvm.Status = StatusWord.DownloadFailed;
                        mdvm.ErrorMessage = ex.Message;
                        if (DownloadingCount > 0) DownloadingCount -= 1;
                        await PersistDownloadAsync(mdvm);
                        RefreshFilteredDownloads();
                    }
                });
                _downloadManagers.Remove(mdvm.Id);
                _downloadPersistTicks.Remove(mdvm.Id);
            }
        });
        WaitingCount -= 1;
        DownloadingCount += 1;
    }

    private void Downloading(MediaDownloadViewModel mdvm)
    {
        // 开始下载
        if (_downloadManagers[mdvm.Id].DownloadStatus.Count > 0)
        {
            var downloadStatus = _downloadManagers[mdvm.Id].DownloadStatus[0];
            mdvm.Speed = downloadStatus.Speed;
            mdvm.SizeStr = downloadStatus.SizeStr;
            mdvm.Progress = downloadStatus.Percentage;
            mdvm.RemainingTime = downloadStatus.RemainingTimeStr;
            mdvm.DownloadedBytes = downloadStatus.Size;
            mdvm.TotalBytes = downloadStatus.TotalSize;
            if (downloadStatus.DownloadType != DownloadType.None)
            {
                mdvm.DownloadStatus = downloadStatus.DownloadType;
                mdvm.OutputFilePath = ResolveOutputFilePath(downloadStatus, mdvm);

                if (mdvm.DownloadStatus == DownloadType.Downloaded)
                {
                    DownloadingCount -= 1;
                    mdvm.Status = StatusWord.Downloaded;
                    mdvm.Progress = 100;
                }
                else if (mdvm.DownloadStatus == DownloadType.DownloadFailed)
                {
                    DownloadingCount -= 1;
                    mdvm.Status = StatusWord.DownloadFailed;
                    mdvm.ErrorMessage = StatusWord.DownloadFailed;
                }
            }

            if (mdvm.DownloadStatus == DownloadType.Downloading && ShouldPersistDownloadTick(mdvm.Id))
                _ = PersistDownloadAsync(mdvm);
        }
    }

    private bool ShouldPersistDownloadTick(int id)
    {
        _downloadPersistTicks.TryGetValue(id, out var tick);
        tick++;
        _downloadPersistTicks[id] = tick;
        return tick == 1 || tick % 5 == 0;
    }

    private async Task PersistDownloadAsync(MediaDownloadViewModel mdvm)
    {
        if (mdvm.DownloadStatus == DownloadType.Downloaded)
        {
            mdvm.OutputFilePath = DownloadFileResolver.ResolveExistingFile(
                mdvm.OutputFilePath,
                mdvm.LocalPath,
                mdvm.Name,
                mdvm.Episode) ?? mdvm.OutputFilePath;
        }

        var mediaDownload = new MediaDownload
        {
            Id = mdvm.Id,
            Source = mdvm.Source,
            Name = mdvm.Name,
            Episode = mdvm.Episode,
            Url = mdvm.Url,
            LocalPath = mdvm.LocalPath,
            IsDownloaded = mdvm.DownloadStatus == DownloadType.Downloaded,
            DownloadStatus = (int)mdvm.DownloadStatus,
            Progress = mdvm.DownloadStatus == DownloadType.Downloaded ? 100 : mdvm.Progress,
            DownloadedBytes = mdvm.DownloadedBytes,
            TotalBytes = mdvm.TotalBytes,
            SizeText = mdvm.SizeStr,
            SpeedText = mdvm.Speed,
            RemainingTimeText = mdvm.RemainingTime,
            ErrorMessage = mdvm.ErrorMessage,
            OutputFilePath = mdvm.OutputFilePath,
            Cover = mdvm.Cover,
            CreateTime = mdvm.CreateTime,
            UpdateTime = DateTime.Now
        };

        await _mediaDownloadTable.UpdateAsync(mediaDownload);
    }

    private static string ToStatusWord(DownloadType downloadType)
    {
        return downloadType switch
        {
            DownloadType.Downloading => StatusWord.Downloading,
            DownloadType.Downloaded => StatusWord.Downloaded,
            DownloadType.DownloadFailed => StatusWord.DownloadFailed,
            _ => StatusWord.Unstarted
        };
    }

    private static string? ResolveOutputFilePath(DownloadStatus downloadStatus, MediaDownloadViewModel mediaDownload)
    {
        var resolvedFilePath = DownloadFileResolver.ResolveExistingFile(
            null,
            downloadStatus.SaveDir,
            downloadStatus.Name,
            mediaDownload.Episode);
        if (!string.IsNullOrWhiteSpace(resolvedFilePath)) return resolvedFilePath;

        if (string.IsNullOrWhiteSpace(downloadStatus.SaveDir) || string.IsNullOrWhiteSpace(downloadStatus.Name)) return mediaDownload.OutputFilePath;
        return Path.Combine(downloadStatus.SaveDir, downloadStatus.Name);
    }

    private void AddDownloadCounts(DownloadType downloadStatus)
    {
        TotalCount += 1;
        if (downloadStatus == DownloadType.None) WaitingCount += 1;
        else if (downloadStatus == DownloadType.Downloading) DownloadingCount += 1;
    }

    private void RemoveDownloadCounts(DownloadType downloadStatus)
    {
        if (TotalCount > 0) TotalCount -= 1;
        if (downloadStatus == DownloadType.None && WaitingCount > 0) WaitingCount -= 1;
        else if (downloadStatus == DownloadType.Downloading && DownloadingCount > 0) DownloadingCount -= 1;
    }

    [RelayCommand]
    private async Task DeleteDownloadTask(MediaDownloadViewModel? task)
    {
        if (task is null) return;
        await _mediaDownloadTable.DeleteByIdAsync(task.Id);
        _downloadManagers.Remove(task.Id);
        _downloadPersistTicks.Remove(task.Id);
        if (ReferenceEquals(_currentDownloadingMVM, task)) _currentDownloadingMVM = null;
        MediaDownloadViewModels.Remove(task);
        RemoveDownloadCounts(task.DownloadStatus);
        RefreshFilteredDownloads();
    }

    [RelayCommand]
    private async Task ClearCompletedTasks()
    {
        var completedTasks = MediaDownloadViewModels
            .Where(task => task.DownloadStatus == DownloadType.Downloaded)
            .ToList();
        foreach (var task in completedTasks)
        {
            await DeleteDownloadTask(task);
        }
    }

    [RelayCommand]
    private async Task ClearFailedTasks()
    {
        var failedTasks = MediaDownloadViewModels
            .Where(task => task.DownloadStatus == DownloadType.DownloadFailed)
            .ToList();
        foreach (var task in failedTasks)
        {
            await DeleteDownloadTask(task);
        }
    }

    [RelayCommand]
    private async Task DownloadAction()
    {
        if (string.IsNullOrEmpty(DownloadName) || string.IsNullOrEmpty(DownloadUrl))
        {
            App.Notification?.Show(new Notification("错误", "请输入下载名称和地址", NotificationType.Error),
                NotificationType.Error);
            return;
        }

        // 外部资源下载
        await AddMediaDownload(DownloadName, DownloadUrl);
    }

    [RelayCommand]
    private async Task SwitchDownloadOrHistory(string tag)
    {
        if (tag != "历史") return;

        await LoadDownloadHistoryFromDbAsync();
    }

    [RelayCommand]
    private async void DeleteFromDbAsync(int id)
    {
        var history = MediaHistoryViewModels.FirstOrDefault(x => x.Id == id);
        if (history == null) return;

        // 遍历文件夹
        var files = Directory.GetFiles(history.LocalPath, "*.mp4", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            if (Path.GetFileName(file).StartsWith(history.Name))
            {
                File.Delete(file);
            }
        }

        await _mediaDownloadTable.DeleteByIdAsync(id);
        MediaHistoryViewModels.Remove(history);
    }
}

public partial class MediaDownloadViewModel : ObservableObject
{
    [ObservableProperty] private string? _localPath; // 本地地址
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _remainingTime = "--:--:--";
    [ObservableProperty] private string _sizeStr = "--/--";
    [ObservableProperty] private string _speed = "0 Bps";
    [ObservableProperty] private string? _status = StatusWord.Unstarted; // 状态 未开始/下载中/下载失败/已完成
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _outputFilePath;
    public DownloadType DownloadStatus { get; set; } = DownloadType.None; // 下载状态
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }

    public int Id { get; set; }
    public string? Source { get; set; }
    public string? Name { get; set; } //电影名
    public string? Episode { get; set; } //剧集
    public string? Url { get; set; } //播放地址
    public string? Cover { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime UpdateTime { get; set; }

    [RelayCommand]
    public void Play()
    {
        var filePath = DownloadFileResolver.ResolveExistingFile(OutputFilePath, LocalPath, Name, Episode);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            App.Notification?.Show(new Notification("错误", "下载文件不存在，无法播放", NotificationType.Error),
                NotificationType.Error);
            return;
        }

        OutputFilePath = filePath;

#if !ANDROID
        // 查找同目录下所有可播放文件，构建剧集列表
        var episodes = BuildLocalEpisodes(filePath);

        var win = new MpvPlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();
        win.Show();
        if (win.DataContext is MpvPlayerWindowModel videoModel)
        {
            var title = string.IsNullOrWhiteSpace(Name) ? Path.GetFileName(filePath) : Name;
            var episode = string.IsNullOrWhiteSpace(Episode) ? title : Episode;
            videoModel.MediaUrl = filePath;
            videoModel.Title = MpvPlayerWindowModel.BuildPlayerTitle(title, episode);
            videoModel.Episodes = new ObservableCollection<EpisodeSubjectItem>(episodes);
            videoModel.ViewHistory = new ViewHistory
            {
                VodId = filePath,
                Name = title,
                Episode = episode,
                Url = filePath,
                Source = string.IsNullOrWhiteSpace(Source) ? "下载" : Source,
                Cover = Cover,
                PlaybackPosition = 0,
                Duration = 0,
                TotalEpisodeCount = episodes.Count,
                IsLocal = true
            };
        }
#else
        var title2 = string.IsNullOrWhiteSpace(Name) ? Path.GetFileName(filePath) : Name;
        var episode2 = string.IsNullOrWhiteSpace(Episode) ? title2 : Episode;
        var viewHistory2 = new ViewHistory
        {
            VodId = filePath,
            Name = title2,
            Episode = episode2,
            Url = filePath,
            Source = string.IsNullOrWhiteSpace(Source) ? "下载" : Source,
            Cover = Cover,
            PlaybackPosition = 0,
            Duration = 0,
            TotalEpisodeCount = 1,
            IsLocal = true
        };
        AndroidVideoPlayerHelper.Play(filePath, $"{title2} - {episode2}", viewHistory2);
#endif
    }

    private List<EpisodeSubjectItem> BuildLocalEpisodes(string currentFilePath)
    {
        var directory = Path.GetDirectoryName(currentFilePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return
            [
                new EpisodeSubjectItem
                {
                    Name = Path.GetFileNameWithoutExtension(currentFilePath),
                    Url = currentFilePath,
                    OutputFilePath = currentFilePath,
                    IsDownloaded = true,
                    Watched = true
                }
            ];
        }

        var playableExtensions = new[] { ".mp4", ".mkv", ".ts", ".m4v", ".mov", ".avi", ".flv", ".wmv", ".webm" };
        var files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(f => playableExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count <= 1)
        {
            return
            [
                new EpisodeSubjectItem
                {
                    Name = Path.GetFileNameWithoutExtension(currentFilePath),
                    Url = currentFilePath,
                    OutputFilePath = currentFilePath,
                    IsDownloaded = true,
                    Watched = true
                }
            ];
        }

        return files.Select(f => new EpisodeSubjectItem
        {
            Name = Path.GetFileNameWithoutExtension(f),
            Url = f,
            OutputFilePath = f,
            IsDownloaded = true,
            Watched = string.Equals(f, currentFilePath, StringComparison.OrdinalIgnoreCase)
        }).ToList();
    }

    [RelayCommand]
    public void OpenFolder()
    {
        if (string.IsNullOrEmpty(LocalPath)) return;

        try
        {
#if ANDROID
            try
            {
                var encodedPath = global::Android.Net.Uri.Encode(LocalPath);
                var uri = global::Android.Net.Uri.Parse($"content://com.android.externalstorage.documents/document/primary:{encodedPath}");
                var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
                intent.SetDataAndType(uri, "resource/folder");
                intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                intent.AddFlags(global::Android.Content.ActivityFlags.GrantReadUriPermission);
                try
                {
                    global::Android.App.Application.Context.StartActivity(intent);
                }
                catch
                {
                    // Fallback: try with generic file manager
                    var fallbackIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
                    fallbackIntent.SetDataAndType(global::Android.Net.Uri.Parse("file://" + LocalPath), "*/*");
                    fallbackIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                    try
                    {
                        global::Android.App.Application.Context.StartActivity(fallbackIntent);
                    }
                    catch
                    {
                        // Last resort: show path
                        App.Notification?.Show(new Notification("下载路径", LocalPath, NotificationType.Information));
                    }
                }
            }
            catch (Exception ex)
            {
                App.Notification?.Show(new Notification("下载路径", LocalPath, NotificationType.Information));
            }
            return;
#else
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", LocalPath);
            }
            else
            {
                Process.Start("open", LocalPath);
            }
#endif
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }
    }
}

public static class DownloadFileResolver
{
    private static readonly string[] PlayableExtensions =
    [
        ".mp4", ".mkv", ".ts", ".m4v", ".mov", ".avi", ".flv", ".wmv", ".webm", ".m4a", ".mp3", ".aac"
    ];

    public static string? ResolveExistingFile(string? outputFilePath, string? localPath, string? name, string? episode)
    {
        if (IsPlayableFile(outputFilePath)) return outputFilePath;
        if (IsPlayableFile(localPath)) return localPath;

        var directory = ResolveDirectory(outputFilePath, localPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return null;

        var files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(IsPlayableFile)
            .Select(file => new FileInfo(file))
            .OrderByDescending(file => Score(file.Name, name, episode))
            .ThenByDescending(file => file.LastWriteTime)
            .ToList();

        return files.FirstOrDefault()?.FullName;
    }

    private static string? ResolveDirectory(string? outputFilePath, string? localPath)
    {
        if (!string.IsNullOrWhiteSpace(localPath) && Directory.Exists(localPath)) return localPath;
        if (!string.IsNullOrWhiteSpace(outputFilePath))
        {
            var directory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrWhiteSpace(directory)) return directory;
        }

        return null;
    }

    private static bool IsPlayableFile(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
               && File.Exists(path)
               && PlayableExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static int Score(string fileName, string? name, string? episode)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(name) && fileName.Contains(name, StringComparison.OrdinalIgnoreCase)) score += 2;
        if (!string.IsNullOrWhiteSpace(episode) && fileName.Contains(episode, StringComparison.OrdinalIgnoreCase)) score += 3;
        return score;
    }

    public static string? GetMediaPath(string localPath, string name)
    {
        if (string.IsNullOrEmpty(localPath)) return null;
        // 遍历文件夹
        var files = Directory.GetFiles(localPath, "*.mp4", SearchOption.TopDirectoryOnly);
        var match = files.FirstOrDefault(file =>
            Path.GetFileName(file).StartsWith(name));
        return match;
    }

}

internal static class StatusWord
{
    public const string Unstarted = "未开始";
    public const string Downloading = "下载中";
    public const string Downloaded = "已完成";
    public const string DownloadFailed = "下载失败";
}