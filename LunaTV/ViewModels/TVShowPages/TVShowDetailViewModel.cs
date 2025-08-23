using System;
using System.Collections.Generic;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using LunaTV.Base.Constants;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Media;
using LunaTV.Views;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowDetailViewModel : ViewModelBase, IDialogContext
{
    public string? VideoName { set; get; }
    public string? SourceName { set; get; }
    public string SourceNameText => $"({AppConifg.ApiSitesConfig[SourceName].Name})";
    public DetailResult VideoDetail { set; get; }

    public bool IsVideoBorderVisible { set; get; }
    public string EpisodesCountText { set; get; }


    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    private void Play(object? episode)
    {
        if (episode is not EpisodeSubject episodeSubject)
        {
            return;
        }

        var win = new PlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();
        if (win.VideoPlayer.DataContext is VideoPlayerViewModel videoModel)
        {
            videoModel.VideoPath = episodeSubject.Url;
            videoModel.VideoName = $"{VideoName} {episodeSubject.Name}";
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

        win.Show();
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