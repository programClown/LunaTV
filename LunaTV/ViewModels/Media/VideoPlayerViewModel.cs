using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LibVLCSharp.Shared;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Services;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.TVShowPages;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;
using Nodify.Avalonia.Shared;
using SqlSugar;
using Ursa.Controls;
using Dialog = LibVLCSharp.Shared.Dialog;

namespace LunaTV.ViewModels.Media;

public partial class VideoPlayerViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc = new();

    public MediaPlayer MediaPlayer { get; }
    public List<string> RateLists { get; } = ["0.5x", "1x", "1.5x", "2x"];

    public bool IsPlay { get; set; }
    public bool IsMuted { get; set; }
    public ViewHistory? ViewHistory { get; set; }

    [ObservableProperty] private ObservableCollection<EpisodeSubjectItem> _episodes;

    [ObservableProperty] private string _videoName = "xxxxxx";
    [ObservableProperty] private Symbol _playIcon = Symbol.PlayFilled;
    [ObservableProperty] private double _seekPosition = 0;
    [ObservableProperty] private bool _canInteractSeekSlider = true;
    [ObservableProperty] private double _maximumSeekValue = 0;
    [ObservableProperty] private Symbol _muteIcon = Symbol.Speaker2Filled;
    [ObservableProperty] private float _volume = 0.5f;
    [ObservableProperty] private string _videoPath;
    [ObservableProperty] private bool _isMultiVideos;
    [ObservableProperty] private string _selectRate = "1x";
    [ObservableProperty] private bool _isVideosKanbanChecked;
    [ObservableProperty] private int _kanBanWidth = 0;
    private bool _isUpdatingFromMedia;
    private bool _isUserSeeking;
    private readonly DispatcherTimer _debounceTimer;
    private readonly SugarRepository<ViewHistory> _viewHistoryTable;

    public VideoPlayerViewModel()
    {
        MediaPlayer = new MediaPlayer(_libVlc);
        MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
        MediaPlayer.LengthChanged += MediaPlayerOnLengthChanged;
        // MediaPlayer.EndReached += MediaPlayerOnEndReached;

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _debounceTimer.Tick += DebounceTimerOnTick;
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
    }

    public void UpdateFromHistory(string source, string vodId, string name)
    {
        Task.Run(async () =>
        {
            var videos = await App.Services.GetRequiredService<MovieTvService>()
                .SearchDetail(source, vodId, AppConifg.AdultApiSitesConfig.ContainsKey(source));
            Episodes = new ObservableCollection<EpisodeSubjectItem>(videos.Episodes.Select(ep =>
                new EpisodeSubjectItem
                {
                    Watched = ep.Name == name,
                    Name = ep.Name,
                    Url = ep.Url,
                }).ToList());
        });
    }

    private void MediaPlayerOnEndReached(object? sender, EventArgs e)
    {
        Stop();
        foreach (var episode in Episodes)
        {
            if (episode.Url == VideoPath)
            {
                if (Episodes.Count > Episodes.IndexOf(episode) + 1)
                {
                    ViewHistory.PlaybackPosition = 0;
                    ViewHistory.Episode = Episodes[Episodes.IndexOf(episode) + 1].Name;
                    ViewHistory.Url = Episodes[Episodes.IndexOf(episode) + 1].Url;

                    VideoPath = Episodes[Episodes.IndexOf(episode) + 1].Url;
                    VideoName = $"{ViewHistory?.Name} {Episodes[Episodes.IndexOf(episode) + 1].Name}";
                    Episodes.ForEach(episode =>
                        episode.Watched = episode.Name == Episodes[Episodes.IndexOf(episode) + 1].Name);
                }
            }
        }
    }

    private void DebounceTimerOnTick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        if (MediaPlayer.IsSeekable)
        {
            MediaPlayer.SeekTo(TimeSpan.FromSeconds(SeekPosition));
        }

        _isUserSeeking = false;
    }

    private void MediaPlayerOnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        MaximumSeekValue = MediaPlayer.Length / 1000.0;
        if (MediaPlayer.Length > 0)
        {
            SeekPosition = ViewHistory?.PlaybackPosition ?? 0;
        }
    }

    [RelayCommand]
    private void Play()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(VideoPath))
        {
            return;
        }

        if (!IsPlay)
        {
            if (!MediaPlayer.IsSeekable)
            {
                using var media = new LibVLCSharp.Shared.Media(_libVlc, VideoPath,
                    Regex.IsMatch(VideoPath, @"^https?://[^\s""']+\.m3u8(?:\?[^\s""']*)?$", RegexOptions.IgnoreCase)
                        ? FromType.FromLocation
                        : FromType.FromPath);
                MediaPlayer.Play(media);
                SelectRate = RateLists[1];
            }
            else
            {
                MediaPlayer.Play();
            }

            PlayIcon = Symbol.PauseFilled;
        }
        else
        {
            PlayIcon = Symbol.PlayFilled;
            Pause();
        }

        IsPlay = !IsPlay;
    }

    [RelayCommand]
    private void KanbanSelect(EpisodeSubjectItem item)
    {
        Stop();
        IsVideosKanbanChecked = false;
        KanBanWidth = 0;
        ViewHistory.PlaybackPosition = 0;
        ViewHistory.Episode = item.Name;
        ViewHistory.Url = item.Url;
        VideoPath = item.Url;
        VideoName = $"{ViewHistory?.Name} {item.Name}";
        Episodes.ForEach(episode => episode.Watched = episode.Name == item.Name);
    }

    public void Stop()
    {
        if (MaximumSeekValue > 0 && ViewHistory is not null)
        {
            ViewHistory.PlaybackPosition = (int)SeekPosition;
            ViewHistory.Duration = (int)MaximumSeekValue;
            _viewHistoryTable.InsertOrUpdate(ViewHistory);
        }

        _debounceTimer.Stop();
        MediaPlayer.Stop();
        IsPlay = false;
        MaximumSeekValue = 0;
        SeekPosition = 0;
        _isUserSeeking = false;
        _isUpdatingFromMedia = false;
        PlayIcon = Symbol.PlayFilled;
    }

    public void Pause()
    {
        MediaPlayer.Pause();
    }

    public void Dispose()
    {
        Stop();
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }

    [RelayCommand]
    private void Previous()
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            await MessageBox.ShowAsync(PlayerWindow.Window, "前面没有了", "打住！", MessageBoxIcon.Warning);
        });
    }

    [RelayCommand]
    private void Next()
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            await MessageBox.ShowAsync(PlayerWindow.Window, "后边没有了", "打住！", MessageBoxIcon.Warning);
        });
    }

    [RelayCommand]
    private void Mute()
    {
        if (!IsMuted)
        {
            MuteIcon = Symbol.SpeakerOffFilled;
        }
        else
        {
            MuteIcon = Symbol.Speaker2Filled;
        }

        IsMuted = !IsMuted;
        MediaPlayer.Mute = IsMuted;
    }

    partial void OnVideoPathChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        Play();
    }

    partial void OnSeekPositionChanged(double value)
    {
        if (MediaPlayer.IsSeekable && !_isUpdatingFromMedia)
        {
            _isUserSeeking = true;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    private void MediaPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        if (_isUserSeeking) return;

        _isUpdatingFromMedia = true;
        SeekPosition = e.Position * MaximumSeekValue;
        _isUpdatingFromMedia = false;
    }


    partial void OnVolumeChanged(float value)
    {
        if (MediaPlayer.IsSeekable)
        {
            MediaPlayer.Volume = (int)(value * 100);
        }
    }

    partial void OnSelectRateChanged(string value)
    {
        MediaPlayer.SetRate(float.Parse(value.Substring(0, value.Length - 1)));
    }

    partial void OnIsVideosKanbanCheckedChanged(bool value)
    {
        KanBanWidth = value ? 300 : 0;
    }
}