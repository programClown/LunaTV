using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using LunaTV.Base.Constants;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Media;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowDetailViewModel : ViewModelBase, IDialogContext
{
    public string? VideoName { set; get; }
    public string? SourceName { set; get; }
    public string SourceNameText => $"({AppConifg.ApiSitesConfig[SourceName].Name})";
    public DetailResult VideoDetail { set; get; }
    public List<EpisodeSubjectItem> Episodes { set; get; } = new();
    public bool IsVideoBorderVisible { set; get; }
    public string EpisodesCountText { set; get; }
    private readonly SugarRepository<ViewHistory> _viewHistoryTable;

    public TVShowDetailViewModel()
    {
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
    }

    public void RefreshUi()
    {
        Episodes = VideoDetail.Episodes.Select(ep => new EpisodeSubjectItem
        {
            Watched = false,
            Name = ep.Name,
            Url = ep.Url,
        }).ToList();
        EpisodesCountText = $"共{Episodes.Count}集";
        var viewHistory = _viewHistoryTable.GetSingle(his =>
            his.VodId == VideoDetail.VodId && his.Source == SourceName && his.Name == VideoName);
        if (viewHistory is not null)
        {
            Episodes[Episodes.IndexOf(Episodes.FirstOrDefault(ep => ep.Name == viewHistory.Episode))].Watched = true;
        }
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    private void Play(object? episode)
    {
        if (episode is not EpisodeSubjectItem episodeSubject)
        {
            return;
        }

        Episodes.ForEach(episode => episode.Watched = episode.Name == episodeSubject.Name);

        var win = new PlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();
        win.Show();
        if (win.VideoPlayer.DataContext is VideoPlayerViewModel videoModel)
        {
            videoModel.VideoPath = episodeSubject.Url;
            videoModel.VideoName = $"{VideoName} {episodeSubject.Name}";
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
                    IsLocal = false,
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
                    IsLocal = false,
                };
            }
        }

        Close();
    }

    [RelayCommand]
    private void CopyLinks()
    {
        App.Notification?.Show(new Notification("复制链接", "未实现呢！", NotificationType.Warning), NotificationType.Warning);
    }

    [RelayCommand]
    private void ReverseVideos()
    {
        App.Notification?.Show(new Notification("倒序排列", "未实现呢！", NotificationType.Warning), NotificationType.Warning);
    }
}

public partial class EpisodeSubjectItem : ObservableObject
{
    [ObservableProperty] private bool _watched; //是否观看
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _url;
}