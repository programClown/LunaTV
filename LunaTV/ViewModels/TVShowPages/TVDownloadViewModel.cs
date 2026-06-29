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
    private MediaDownloadViewModel? _currentDownloadingMvm;

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
        MediaHistoryViewModels = new ObservableCollection<MediaDownloadViewModel>();

        _downloadTimer = new DispatcherTimer
            (TimeSpan.FromSeconds(1), DispatcherPriority.Background, DownloadTimerOnTick);
        _downloadTimer.Start();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await LoadUnDownloadFromDbAsync();
            await LoadDownloadHistoryFromDbAsync();
        });
    }

    public ObservableCollection<MediaDownloadViewModel> MediaDownloadViewModels { get; set; }
    public ObservableCollection<MediaDownloadViewModel> MediaHistoryViewModels { get; set; }

    // 从数据库加载下载任务
    private async Task LoadUnDownloadFromDbAsync()
    {
        var unDownloads = await _mediaDownloadTable.GetListAsync(x => !x.IsDownloaded);
        foreach (var downloadTask in unDownloads)
        {
            if (!MediaDownloadViewModels.Any(x => x.Url == downloadTask.Url))
            {
                MediaDownloadViewModels.Add(new MediaDownloadViewModel
                {
                    Id = downloadTask.Id,
                    Name = downloadTask.Name,
                    Episode = downloadTask.Episode,
                    Url = downloadTask.Url,
                    DownloadStatus = DownloadType.None,
                    LocalPath = downloadTask.LocalPath,
                    Status = StatusWord.Unstarted,
                    Progress = 0,
                    UpdateTime = downloadTask.UpdateTime
                });
                WaitingCount += 1;
                TotalCount += 1;
            }
        }

        _isInitialized = true;
        if (!File.Exists(GlobalDefine.FFmpegPath))
        {
            App.Notification?.Show(new Notification("错误", "FFmpeg路径配置错误", NotificationType.Error),
                NotificationType.Error);
        }
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
                Status = string.IsNullOrEmpty(MediaDownloadViewModel.GetMediaPath(download.LocalPath, download.Name))
                    ? StatusWord.DownloadFailed
                    : StatusWord.Downloaded,
                Progress = 100,
                UpdateTime = download.UpdateTime
            });
        }
    }

    private async void DownloadTimerOnTick(object? sender, EventArgs e)
    {
        if (!_isInitialized || !File.Exists(GlobalDefine.FFmpegPath))
        {
            return;
        }

        try
        {
            if (_currentDownloadingMvm is null)
            {
                if (MediaDownloadViewModels.Count > 0)
                {
                    _currentDownloadingMvm =
                        MediaDownloadViewModels.FirstOrDefault(x => x.DownloadStatus == DownloadType.None);
                    if (_currentDownloadingMvm != null)
                    {
                        Console.WriteLine($"开始下载：{_currentDownloadingMvm.Name}");
                        PreDownload(_currentDownloadingMvm);
                    }
                }
            }
            else
            {
                if (_currentDownloadingMvm.DownloadStatus == DownloadType.Downloading)
                {
                    Console.WriteLine($"下载中：{_currentDownloadingMvm.Name}");
                    // 刷新下载进度
                    Downloading(_currentDownloadingMvm);
                }
                else
                {
                    _currentDownloadingMvm = null;
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    ~TVDownloadViewModel()
    {
        _downloadTimer.Stop();
    }

    public async Task AddMediaDownload(string name, string url, string folder = "")
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
            Source = string.Empty,
            Name = name,
            Episode = string.Empty,
            Url = url,
            IsDownloaded = false,
            LocalPath = Path.Combine(GlobalDefine.DownloadPath, folder)
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
            Name = name,
            Episode = string.Empty,
            Url = url,
            DownloadStatus = DownloadType.None,
            LocalPath = Path.Combine(GlobalDefine.DownloadPath, folder),
            Status = StatusWord.Unstarted,
            Progress = 0,
            UpdateTime = DateTime.Now
        });

        TotalCount += 1;
        WaitingCount += 1;
    }

    private void PreDownload(MediaDownloadViewModel mdvm)
    {
        mdvm.DownloadStatus = DownloadType.Downloading;
        _downloadManagers[mdvm.Id] = new DownloadManager();
        mdvm.Status = StatusWord.Downloading;
        Task.Run(async () =>
        {
            var result = await _downloadManagers[mdvm.Id].DownloadAsync(mdvm.Url!, mdvm.LocalPath!, mdvm.Name!);
            Downloading(mdvm); // 刷新下载进度
            var md = new MediaDownload
            {
                Id = mdvm.Id,
                Source = string.Empty,
                Name = mdvm.Name,
                Episode = mdvm.Episode,
                Url = mdvm.Url,
                IsDownloaded = true,
                LocalPath = mdvm.LocalPath
            };
            await _mediaDownloadTable.UpdateAsync(md);
            _downloadManagers.Remove(mdvm.Id);
            mdvm.DownloadStatus = result ? DownloadType.Downloaded : DownloadType.DownloadFailed;
            mdvm.Status = result ? StatusWord.Downloaded : StatusWord.DownloadFailed;
            DownloadingCount -= 1;
            _currentDownloadingMvm =
                MediaDownloadViewModels.FirstOrDefault(x => x.DownloadStatus == DownloadType.None);
            if (_currentDownloadingMvm != null)
            {
                Console.WriteLine($"开始下载：{_currentDownloadingMvm.Name}");
                PreDownload(_currentDownloadingMvm);
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
            mdvm.Speed = _downloadManagers[mdvm.Id].DownloadStatus[0].Speed;
            mdvm.SizeStr = _downloadManagers[mdvm.Id].DownloadStatus[0].SizeStr;
            mdvm.Progress = (int)_downloadManagers[mdvm.Id].DownloadStatus[0].Percentage;
            mdvm.RemainingTime = _downloadManagers[mdvm.Id].DownloadStatus[0].RemainingTimeStr;
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
        try
        {
            var files = Directory.GetFiles(history.LocalPath, "*.mp4", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                if (Path.GetFileName(file).StartsWith(history.Name))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }


        await _mediaDownloadTable.DeleteByIdAsync(id);
        MediaHistoryViewModels.Remove(history);
    }
}

public partial class MediaDownloadViewModel : ObservableObject
{
    [ObservableProperty] private string? _localPath; // 本地地址
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _remainingTime = "--:--:--";
    [ObservableProperty] private string _sizeStr = "--:--/--:--";
    [ObservableProperty] private string _speed = "0:00MBps";
    [ObservableProperty] private string? _status = StatusWord.Unstarted; // 状态 未开始/下载中/下载失败/已完成
    public DownloadType DownloadStatus { get; set; } = DownloadType.None; // 下载状态 

    public int Id { get; set; }
    public string? Name { get; set; } //电影名
    public string? Episode { get; set; } //剧集
    public string? Url { get; set; } //播放地址
    public DateTime UpdateTime { get; set; }

    [RelayCommand]
    private void OpenFolder()
    {
        if (string.IsNullOrEmpty(LocalPath)) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", LocalPath);
            }
            else
            {
                Process.Start("open", LocalPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static string? GetMediaPath(string localPath, string name)
    {
        if (string.IsNullOrEmpty(localPath)) return null;
        // 遍历文件夹
        try
        {
            var files = Directory.GetFiles(localPath, "*.mp4", SearchOption.TopDirectoryOnly);
            var match = files.FirstOrDefault(file =>
                Path.GetFileName(file).StartsWith(name));
            return match;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [RelayCommand]
    private void Play()
    {
        var path = GetMediaPath(LocalPath, Name);
        if (string.IsNullOrEmpty(path)) return;

        var win = new MpvPlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();
        win.Show();
        if (win.DataContext is MpvPlayerWindowModel videoModel)
        {
            videoModel.MediaUrl = path;
            videoModel.Title = Name;
        }
    }
}

internal static class StatusWord
{
    public const string Unstarted = "未开始";
    public const string Downloading = "下载中";
    public const string Downloaded = "已完成";
    public const string DownloadFailed = "下载失败";
}