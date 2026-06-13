using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowDetailViewModel : ViewModelBase, IDialogContext
{
    private readonly SugarRepository<ViewHistory> _viewHistoryTable;
    private readonly SugarRepository<MediaDownload> _mediaDownloadTable;
    [ObservableProperty] private bool _isDownloadingSelected;
    [ObservableProperty] private int _selectedEpisodeCount;


    public TVShowDetailViewModel()
    {
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
        _mediaDownloadTable = App.Services.GetRequiredService<SugarRepository<MediaDownload>>();
    }

    public string? VideoName { get; set; }
    public string? SourceName { get; set; }
    public string? Cover { get; set; }
    public string SourceNameText => GetSourceNameText();
    public DetailResult VideoDetail { get; set; }
    public List<EpisodeSubjectItem> Episodes { get; set; } = new();
    public bool IsVideoBorderVisible { get; set; }
    public string EpisodesCountText { get; set; }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    private string GetSourceNameText()
    {
        if (string.IsNullOrWhiteSpace(SourceName)) return string.Empty;
        if (AppConifg.ApiSitesConfig.TryGetValue(SourceName, out var site)) return $"({site.Name})";
        if (AppConifg.AdultApiSitesConfig.TryGetValue(SourceName, out var adultSite)) return $"({adultSite.Name})";

        return $"({SourceName})";
    }

    public async Task RefreshUiAsync()
    {
        Episodes = VideoDetail.Episodes?.Select(ep => new EpisodeSubjectItem
        {
            Watched = false,
            Name = ep.Name,
            Url = ep.Url,
            IsSelected = true // 默认全部选中
        }).ToList() ?? [];
        EpisodesCountText = $"共{Episodes.Count}集";
        await RefreshDownloadStatusAsync();
        var viewHistory = _viewHistoryTable.GetSingle(his =>
            his.VodId == VideoDetail.VodId && his.Source == SourceName && his.Name == VideoName);
        if (viewHistory is not null)
        {
            var watchedEpisode = Episodes.FirstOrDefault(ep => ep.Name == viewHistory.Episode);
            if (watchedEpisode is not null) watchedEpisode.Watched = true;
        }

        SelectChanged();
    }

    private async Task RefreshDownloadStatusAsync()
    {
        var urls = Episodes
            .Select(episode => episode.Url)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct()
            .ToList();
        if (urls.Count == 0) return;

        var downloadRecords = await _mediaDownloadTable.Context.Queryable<MediaDownload>()
            .Where(download => download.Url != null && urls.Contains(download.Url) && download.IsDownloaded)
            .ToListAsync();
        var downloadRecordsByUrl = downloadRecords
            .Where(download => !string.IsNullOrWhiteSpace(download.Url))
            .GroupBy(download => download.Url!)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(download => download.UpdateTime).First());
        foreach (var episode in Episodes)
        {
            if (string.IsNullOrWhiteSpace(episode.Url) || !downloadRecordsByUrl.TryGetValue(episode.Url, out var download)) continue;
            var outputFilePath = DownloadFileResolver.ResolveExistingFile(download.OutputFilePath, download.LocalPath, download.Name, download.Episode)
                                 ?? download.OutputFilePath;
            episode.IsDownloaded = true;
            episode.OutputFilePath = outputFilePath;
            if (!string.IsNullOrWhiteSpace(outputFilePath) && download.OutputFilePath != outputFilePath)
            {
                download.OutputFilePath = outputFilePath;
                await _mediaDownloadTable.UpdateAsync(download);
            }
        }
    }

    [RelayCommand]
    private async Task Play(object? episode)
    {
        if (episode is not EpisodeSubjectItem episodeSubject) return;

        Episodes.ForEach(episode => episode.Watched = episode.Name == episodeSubject.Name);

#if !ANDROID
        var win = new MpvPlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();
        win.Show();
        if (win.DataContext is MpvPlayerWindowModel videoModel)
        {
            var mediaUrl = DownloadFileResolver.ResolveExistingFile(
                               episodeSubject.OutputFilePath,
                               Path.GetDirectoryName(episodeSubject.OutputFilePath ?? string.Empty),
                               VideoName,
                               episodeSubject.Name)
                           ?? episodeSubject.Url;
            episodeSubject.OutputFilePath = mediaUrl;
            videoModel.MediaUrl = mediaUrl;
            videoModel.Title = MpvPlayerWindowModel.BuildPlayerTitle(VideoName, episodeSubject.Name);
            videoModel.Episodes = new ObservableCollection<EpisodeSubjectItem>(Episodes);

            var cover = string.IsNullOrWhiteSpace(Cover) ? VideoDetail.Cover : Cover;
            var viewHistory = _viewHistoryTable.GetSingle(his =>
                his.VodId == VideoDetail.VodId && his.Source == SourceName && his.Name == VideoName);
            if (viewHistory is not null)
            {
                var isSameEpisode = viewHistory.Episode == episodeSubject.Name;
                videoModel.ViewHistory = new ViewHistory
                {
                    Id = viewHistory.Id,
                    VodId = VideoDetail.VodId,
                    Name = VideoName,
                    Episode = episodeSubject.Name,
                    Url = mediaUrl,
                    Source = SourceName,
                    Cover = cover,
                    PlaybackPosition = isSameEpisode ? viewHistory.PlaybackPosition : 0,
                    Duration = isSameEpisode ? viewHistory.Duration : 0,
                    TotalEpisodeCount = VideoDetail.Episodes.Count,
                    IsLocal = false,
                    CreateTime = viewHistory.CreateTime
                };
            }
            else
            {
                videoModel.ViewHistory = new ViewHistory
                {
                    VodId = VideoDetail.VodId,
                    Name = VideoName,
                    Episode = episodeSubject.Name,
                    Url = mediaUrl,
                    Source = SourceName,
                    Cover = cover,
                    PlaybackPosition = 0,
                    Duration = 0,
                    TotalEpisodeCount = VideoDetail.Episodes.Count,
                    IsLocal = false
                };
            }
        }
#else
        var mediaUrl2 = DownloadFileResolver.ResolveExistingFile(
                            episodeSubject.OutputFilePath,
                            Path.GetDirectoryName(episodeSubject.OutputFilePath ?? string.Empty),
                            VideoName,
                            episodeSubject.Name)
                        ?? episodeSubject.Url;
        if (string.IsNullOrEmpty(mediaUrl2))
        {
            App.Notification?.Show(new Notification("错误", "无法获取播放地址", NotificationType.Error));
        }
        else
        {
            var cover = string.IsNullOrWhiteSpace(Cover) ? VideoDetail.Cover : Cover;
            var viewHistory = _viewHistoryTable.GetSingle(his =>
                his.VodId == VideoDetail.VodId && his.Source == SourceName && his.Name == VideoName);
            var history = viewHistory is not null
                ? new ViewHistory
                {
                    Id = viewHistory.Id,
                    VodId = VideoDetail.VodId,
                    Name = VideoName,
                    Episode = episodeSubject.Name,
                    Url = mediaUrl2,
                    Source = SourceName,
                    Cover = cover,
                    PlaybackPosition = viewHistory.Episode == episodeSubject.Name ? viewHistory.PlaybackPosition : 0,
                    Duration = viewHistory.Episode == episodeSubject.Name ? viewHistory.Duration : 0,
                    TotalEpisodeCount = VideoDetail.Episodes.Count,
                    IsLocal = false,
                    CreateTime = viewHistory.CreateTime
                }
                : new ViewHistory
                {
                    VodId = VideoDetail.VodId,
                    Name = VideoName,
                    Episode = episodeSubject.Name,
                    Url = mediaUrl2,
                    Source = SourceName,
                    Cover = cover,
                    PlaybackPosition = 0,
                    Duration = 0,
                    TotalEpisodeCount = VideoDetail.Episodes.Count,
                    IsLocal = false
                };

            var title = $"{VideoName} - {episodeSubject.Name}";
            AndroidVideoPlayerHelper.Play(mediaUrl2, title, history);
        }
#endif

        Close();
    }

    [RelayCommand]
    private async Task CopyLinks()
    {
        var CopyMediaSubject = new CopyMediaSubject
        {
            Name = VideoName,
            Medias = Episodes.Select(ep => new CopyMediaDetail
            {
                Url = ep.Url,
                Episode = ep.Name
            }).ToList()
        };
        await App.Clipboard.SetTextAsync(JsonSerializer.Serialize(CopyMediaSubject,
            new JsonSerializerOptions
            {
                WriteIndented = true, // 美化输出（缩进）
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 支持中文字符
            }));
        App.Notification?.Show(new Notification("复制链接", $"成功复制{Episodes.Count}个链接到剪切板",
                NotificationType.Success),
            NotificationType.Success);
    }

    /// <summary>
    ///     下载选中的剧集
    /// </summary>
    [RelayCommand]
    private async Task DownloadSelected()
    {
        if (!File.Exists(GlobalDefine.FFmpegPath))
        {
            App.Notification?.Show(new Notification("错误", "FFmpeg路径配置错误", NotificationType.Error),
                NotificationType.Error);
            return;
        }

        var selectedEpisodes = Episodes.Where(ep => ep.IsSelected);

        var tvdownloadVm = App.Services.GetRequiredService<TVDownloadViewModel>();

        foreach (var episode in selectedEpisodes)
        {
            if (Episodes.Count > 1)
            {
                await tvdownloadVm.AddMediaDownload($"{VideoName}-{episode.Name}", episode.Url, VideoName, SourceName ?? string.Empty, Cover ?? VideoDetail.Cover);
            }
            else
            {
                await tvdownloadVm.AddMediaDownload($"{VideoName}-{episode.Name}", episode.Url, source: SourceName ?? string.Empty, cover: Cover ?? VideoDetail.Cover);
            }

            episode.IsDownloaded = true;
        }

        IsDownloadingSelected = false;
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        var allSelected = Episodes.Any(e => e.IsSelected);
        foreach (var episode in Episodes) episode.IsSelected = !allSelected;

        SelectChanged();
    }

    /// <summary>
    ///     选择剧集
    /// </summary>
    [RelayCommand]
    private void SelectChanged()
    {
        SelectedEpisodeCount = Episodes.Count(e => e.IsSelected);
    }
}

public partial class EpisodeSubjectItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private string? _outputFilePath;
    [ObservableProperty] private bool _watched; //是否观看
}