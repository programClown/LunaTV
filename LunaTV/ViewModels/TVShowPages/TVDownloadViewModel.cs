using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using M3U8Download;
using Microsoft.Extensions.DependencyInjection;
using N_m3u8DL_RE.Util;
using ReactiveUI;
using SqlSugar;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVDownloadViewModel : ViewModelBase
{
    [ObservableProperty] private string _downloadUrl = "https://vod.360zyx.vip/20250708/7T2xjBRd/index.m3u8";
    [ObservableProperty] private string _speed = "0:00MBps";
    [ObservableProperty] private string _sizeStr = "--:--/--:--";
    [ObservableProperty] private string _remainingTime = "--:--:--";

    [ObservableProperty] private int _downloadingCount = 0;
    [ObservableProperty] private int _waitingCount = 0;
    [ObservableProperty] private int _totalCount = 0;
    private readonly SugarRepository<MediaDownload> _mediaDownloadTable;

    private Task _downloadTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public DataGridCollectionView MediaDownloads { get; set; }
    public ObservableCollection<MediaDownloadViewModel> MediaDownloadViewModels { get; set; }
    private Queue<int> WaitList { get; set; }

    private DownloadManager? _downloadManager;

    public TVDownloadViewModel()
    {
        _mediaDownloadTable = App.Services.GetRequiredService<SugarRepository<MediaDownload>>();
        MediaDownloadViewModels = new ObservableCollection<MediaDownloadViewModel>();

        MediaDownloads = new DataGridCollectionView(MediaDownloadViewModels);
        MediaDownloads.GroupDescriptions.Add(new DataGridPathGroupDescription("Name"));

        _downloadManager = new DownloadManager();
        _downloadManager.SetFFmpegPath("C:\\Users\\Austin\\Downloads\\ffmpeg.exe");

        // check ffmpeg

        // task
        WaitList = new Queue<int>([1, 2, 3]);
        _downloadTask = RunDownloadPeriodicTask();
    }

    ~TVDownloadViewModel()
    {
        _cancellationTokenSource.Cancel();
        _downloadTask.Wait();
    }

    private async Task RunDownloadPeriodicTask()
    {
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (WaitList.Count > 0)
                {
                    int index = WaitList.Dequeue();
                    var mvm = MediaDownloadViewModels.FirstOrDefault(x => x.Id == index);
                    if (mvm != null) await Downloading(mvm);
                }

                WaitingCount = WaitList.Count;

                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void AddTvDownload(string name, string url, string episode, bool check = true)
    {
        bool isDownload = false;
        int id = 0;
        if (check)
        {
            var mediaDownload = _mediaDownloadTable.GetSingle(md => md.Url == url);
            if (mediaDownload != null)
            {
                id = mediaDownload.Id;
                isDownload = mediaDownload.IsDownloaded;
            }
        }

        MediaDownloadViewModels.Add(new MediaDownloadViewModel
        {
            Id = id,
            Name = name,
            Episode = episode,
            Url = url,
            IsDownloaded = isDownload,
            LocalPath = Path.Combine(GlobalDefine.DownloadPath, name),
            ActionIndicate = false,
            ActionText = isDownload ? "重新下载" : "开始",
            Status = "未开始",
            StatusIndicate = false,
            Progress = 0,
        });

        var md = new MediaDownload()
        {
            Id = id,
            Source = string.Empty,
            Name = name,
            Episode = episode,
            Url = url,
            IsDownloaded = isDownload,
            LocalPath = Path.Combine(GlobalDefine.DownloadPath, name),
        };

        _mediaDownloadTable.InsertOrUpdate(md);

        TotalCount += 1;
        WaitingCount += 1;
        if (!isDownload)
        {
            WaitList.Enqueue(md.Id);
        }
    }

    public void AddMovieDownload(string name, string url, bool check = true)
    {
        AddTvDownload(name, url, String.Empty, check);
    }

    private async Task<bool> Downloading(MediaDownloadViewModel mdvm)
    {
        // 开始下载
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };

        timer.Tick += (_, _) =>
        {
            if (_downloadManager!.DownloadStatus.Count > 0)
            {
                Speed = _downloadManager.DownloadStatus[0].speed;
                SizeStr = _downloadManager.DownloadStatus[0].sizeStr;
                RemainingTime = _downloadManager.DownloadStatus[0].remainingTimeStr;
            }
        };
        DownloadingCount = 1;
        timer.Start();
        var fileName = string.IsNullOrEmpty(mdvm.Episode) ? $"{mdvm.Name}-{mdvm.Episode}" : $"{mdvm.Name}";
        var result = await _downloadManager!.DownloadAsync(mdvm.Url!, mdvm.LocalPath!, fileName);
        timer.Stop();
        if (_downloadManager!.DownloadStatus.Count > 0)
        {
            Speed = _downloadManager.DownloadStatus[0].speed;
            SizeStr = _downloadManager.DownloadStatus[0].sizeStr;
            RemainingTime = _downloadManager.DownloadStatus[0].remainingTimeStr;
        }

        DownloadingCount = 0;
        return result;
    }

    [RelayCommand]
    private void DownloadAction()
    {
        // 外部资源下载
        AddMovieDownload(OtherUtil.GetFileNameFromInput(DownloadUrl), DownloadUrl, true);
        TotalCount += 1;
        WaitingCount += 1;
    }
}

public partial class MediaDownloadViewModel : ObservableObject
{
    public int Id { get; set; }
    public string? Name { get; set; } //电影名
    public string? Episode { get; set; } //剧集
    public string? Url { get; set; } //播放地址
    [ObservableProperty] private string? _localPath; // 本地地址
    public bool IsDownloaded { get; set; } // 本地地址
    [ObservableProperty] private bool _actionIndicate; // 动作指示
    [ObservableProperty] private string? _actionText = "开始"; // 状态 开始/暂停/重新下载
    [ObservableProperty] private string? _status = "未开始";
    [ObservableProperty] private bool _statusIndicate; // 状态指示
    [ObservableProperty] private int _progress;
    public DateTime UpdateTime { get; set; }

    partial void OnActionTextChanged(string? value)
    {
        Status = value switch
        {
            "开始" => "未开始",
            "暂停" => "下载中",
            "重新下载" => "已完成",
            _ => "未开始"
        };

        IsDownloaded = Status == "已完成";
        ActionIndicate = Status != "未开始";
        StatusIndicate = Status == "下载中";
    }

    [RelayCommand]
    private void DownloadAction()
    {
        ActionText = ActionText switch
        {
            "开始" => "暂停",
            "暂停" => "重新下载",
            "重新下载" => "开始",
            _ => "开始"
        };
    }
}