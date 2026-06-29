using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    [ObservableProperty] private bool _isDownloadingSelected;
    [ObservableProperty] private bool _isWebPlay;
    [ObservableProperty] private int _selectedEpisodeCount;


    public TVShowDetailViewModel()
    {
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
    }

    public string? VideoName { get; set; }
    public string? SourceName { get; set; }
    public string SourceNameText => $"({AppConifg.ApiSitesConfig[SourceName].Name})";
    public DetailResult VideoDetail { get; set; }
    public List<EpisodeSubjectItem> Episodes { get; set; } = new();
    public bool IsVideoBorderVisible { get; set; }
    public string EpisodesCountText { get; set; }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    public void RefreshUi()
    {
        Episodes = VideoDetail.Episodes.Select(ep => new EpisodeSubjectItem
        {
            Watched = false,
            Name = ep.Name,
            Url = ep.Url,
            IsSelected = true // 默认全部选中
        }).ToList();
        EpisodesCountText = $"共{Episodes.Count}集";
        var viewHistory = _viewHistoryTable.GetSingle(his =>
            his.VodId == VideoDetail.VodId && his.Source == SourceName && his.Name == VideoName);
        if (viewHistory is not null)
        {
            Episodes[Episodes.IndexOf(Episodes.FirstOrDefault(ep => ep.Name == viewHistory.Episode))].Watched = true;
        }

        SelectChanged();
    }

    [RelayCommand]
    private void Play(object? episode)
    {
        if (episode is not EpisodeSubjectItem episodeSubject) return;

        if (IsWebPlay)
        {
            WebPlay(episodeSubject);
            return;
        }

        Episodes.ForEach(episode => episode.Watched = episode.Name == episodeSubject.Name);

        var win = new MpvPlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();
        win.Show();
        if (win.DataContext is MpvPlayerWindowModel videoModel)
        {
            videoModel.MediaUrl = episodeSubject.Url;
            videoModel.Title = $"{VideoName} {episodeSubject.Name}";
            videoModel.Episodes = new ObservableCollection<EpisodeSubjectItem>(Episodes);

            var viewHistory = _viewHistoryTable.GetSingle(his =>
                his.VodId == VideoDetail.VodId && his.Source == VideoDetail.Source && his.Name == VideoName);
            if (viewHistory is not null)
            {
                videoModel.ViewHistory = new ViewHistory
                {
                    Id = viewHistory.Id,
                    VodId = VideoDetail.VodId,
                    Name = VideoName,
                    Episode = episodeSubject.Name,
                    Url = episodeSubject.Url,
                    Source = VideoDetail.Source,
                    PlaybackPosition = viewHistory.PlaybackPosition,
                    Duration = 0,
                    TotalEpisodeCount = VideoDetail.Episodes.Count,
                    IsLocal = false
                };
            }
            else
            {
                videoModel.ViewHistory = new ViewHistory
                {
                    VodId = VideoDetail.VodId,
                    Name = VideoName,
                    Episode = episodeSubject.Name,
                    Url = episodeSubject.Url,
                    Source = VideoDetail.Source,
                    PlaybackPosition = 0,
                    Duration = 0,
                    TotalEpisodeCount = VideoDetail.Episodes.Count,
                    IsLocal = false
                };
            }
        }

        Close();
    }

    private void WebPlay(EpisodeSubjectItem episodeSubject)
    {
        try
        {
            Process.Start(new ProcessStartInfo(episodeSubject.Url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            App.Notification?.Show(new Notification("Web播放", $"无法打开浏览器：{ex.Message}", NotificationType.Error),
                NotificationType.Error);
        }
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
                await tvdownloadVm.AddMediaDownload(episode.Name, episode.Url, VideoName);
            }
            else
            {
                await tvdownloadVm.AddMediaDownload($"{VideoName}-{episode.Name}", episode.Url);
            }
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
    [ObservableProperty] private bool _watched; //是否观看
}