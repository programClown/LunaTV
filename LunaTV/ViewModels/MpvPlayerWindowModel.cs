using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.LibMpv;
using HanumanInstitute.LibMpv.Core;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.TVShowPages;
using LunaTV.Views;
using LunaTV.Views.Media;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace LunaTV.ViewModels;

public class SpeedMenuItemViewModel
{
    public string? Header { get; set; }
    public ICommand? Command { get; set; }
    public double CommandParameter { get; set; }
}

public partial class MpvPlayerWindowModel : ViewModelBase, IDisposable
{
    [ObservableProperty] private string _mediaUrl = "https://vip.dytt-luck.com/20250827/19457_e0c4ac2b/index.m3u8";
    [ObservableProperty] private int _volume = 70;
    [ObservableProperty] private double _speed = 1.0f; //0.5,1.0,1.5,2.0,2.5,3.0,3.5,4.0
    [ObservableProperty] private string _speedText = "1x";
    [ObservableProperty] private bool _loop; //循环播放
    [ObservableProperty] private bool _isMediaLoaded;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private TimeSpan _position = TimeSpan.Zero;
    [ObservableProperty] private TimeSpan _duration = TimeSpan.FromSeconds(1);
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private string? _title = "无";
    [ObservableProperty] private int _kanBanWidth;
    [ObservableProperty] private bool _isVideosKanbanChecked;

    public Window? Window { get; set; }

    private readonly LoadingWaitViewModel _loadingWaitViewModel = new();

    public IList<SpeedMenuItemViewModel> SpeedMenuItems { get; } = new List<SpeedMenuItemViewModel>();

    public MpvContext Mpv { get; set; } = default!;
    public WindowNotificationManager? Notification { get; set; }

    // /// <summary>
    // /// Occurs after the media player is initialized.
    // /// </summary>
    // public event EventHandler? MediaPlayerInitialized;
    private bool _isLoaded;
    private bool _isSettingPosition;

    /// <summary>
    /// Gets or sets whether the user is dragging the seek bar.
    /// </summary>
    public bool IsSeekBarPressed { get; set; }

    public MpvPlayerWindowModel()
    {
        for (int i = 8; i >= 1; i--)
        {
            SpeedMenuItems.Add(
                new SpeedMenuItemViewModel()
                {
                    Header = $"{i * 0.5}x",
                    Command = SpeedChangeCommand,
                    CommandParameter = i * 0.5,
                }
            );
        }

        DbServiceInit();
    }

    public async Task OnWindowLoaded()
    {
        await Task.Delay(100); // Fails to load if we don't give a slight delay.

        Mpv!.FileLoaded += PlayerFileLoaded;
        Mpv.EndFile += PlayerEndFile;
        Mpv.TimePos.Changed += PlayerPositionChanged;

        var options = new MpvAsyncOptions { WaitForResponse = false };
        await Mpv.Volume.SetAsync(Volume, options);
        await Mpv.Speed.SetAsync(Speed, options);
        await Mpv.LoopFile.SetAsync(Loop ? "yes" : "no", options);
    }

    private PlaybackStatus _status;

    public PlaybackStatus Status
    {
        get => _status;
        protected set
        {
            SetProperty(ref _status, value);
            var text = _status switch
            {
                PlaybackStatus.Loading => "Loading...",
                PlaybackStatus.Playing => Path.GetFileName(MediaUrl),
                PlaybackStatus.Error => "Error loading media",
                _ => ""
            };

            Notification?.Show(new Notification("播放信息", text, NotificationType.Information),
                NotificationType.Information);
        }
    }

    public async Task PlayPause()
    {
        if (string.IsNullOrEmpty(MediaUrl) || Design.IsDesignMode)
        {
            return;
        }

        if (!_isLoaded)
        {
            await Mpv!.Stop().InvokeAsync();
            await Mpv.Pause.SetAsync(false);
            if (!string.IsNullOrEmpty(MediaUrl))
            {
                _ = Loading();
                await Mpv.LoadFile(MediaUrl!).InvokeAsync();
                IsPlaying = true;
                _isLoaded = true;
                MediaPlayerOnLoaded();
            }
            else
            {
                IsPlaying = false;
            }
        }
        else
        {
            await Mpv!.Pause.SetAsync(IsPlaying);
            IsPlaying = !IsPlaying;
        }
    }

    private void Stoped()
    {
        if (string.IsNullOrEmpty(MediaUrl) && !IsMediaLoaded)
        {
            return;
        }

        MediaUrl = string.Empty;
        _isLoaded = false;
        IsPlaying = false;
        Status = PlaybackStatus.Stopped;
        IsMediaLoaded = false;
        Duration = TimeSpan.FromSeconds(1);
        Position = TimeSpan.Zero;
    }

    public void Stop()
    {
        Mpv.Pause.Set(false);
        Mpv!.Stop().Invoke();
        SpeedChange(1.0f);
        SaveViewHistory();
        Stoped();
    }

    public void Seek(int seconds)
    {
        if (!IsMediaLoaded)
        {
            return;
        }

        var newPos = Position.Add(TimeSpan.FromSeconds(seconds));
        if (newPos < TimeSpan.Zero)
        {
            newPos = TimeSpan.Zero;
        }
        else if (newPos > Duration)
        {
            newPos = Duration;
        }

        if (newPos != Position)
        {
            // Position = newPos;
            lock (Mpv)
            {
                Mpv.TimePos.Set(newPos.TotalSeconds);
            }
        }
    }

    public void ChangeVolume(int value)
    {
        int newVolume = Volume + value;
        if (newVolume < 0)
        {
            newVolume = 0;
        }
        else if (newVolume > 100)
        {
            newVolume = 100;
        }

        Volume = newVolume;
    }

    [RelayCommand]
    private void GoHead()
    {
        Position = TimeSpan.Zero;
        if (IsMediaLoaded)
        {
            lock (Mpv!)
            {
                Mpv.TimePos.Set(0);
            }
        }
    }

    [RelayCommand]
    private void GoTail()
    {
        Position = TimeSpan.FromSeconds(Duration.TotalSeconds - 1);
        if (IsMediaLoaded)
        {
            lock (Mpv!)
            {
                // var pos = TimeSpan.FromTicks(Position.Ticks);
                Mpv.TimePos.Set(Position.TotalSeconds);
            }
        }
    }

    [RelayCommand]
    private void Screenshot()
    {
        if (IsMediaLoaded)
        {
            var path = Path.Combine(GlobalDefine.DownloadPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Mpv.ScreenshotToFile(Path.Combine(path, $"{DateTime.Now:yyyyMMddHHmmssfff}.png"))
                .Invoke();
            Notification?.Show(new Notification("截图已保存到", path, NotificationType.Information),
                NotificationType.Information);
        }
    }

    [RelayCommand]
    private void Mute()
    {
        IsMuted = !IsMuted;
        SaveMute();
    }

    [RelayCommand]
    private void SpeedChange(double value)
    {
        SpeedText = $"{value}x";
        Speed = value;
    }

    [RelayCommand]
    private void KanbanSelect(EpisodeSubjectItem item)
    {
        Stop();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(1000);
            IsVideosKanbanChecked = false;
            KanBanWidth = 0;
            ViewHistory.PlaybackPosition = 0;
            ViewHistory.Episode = item.Name;
            ViewHistory.Url = item.Url;
            MediaUrl = item.Url;
            Title = $"{ViewHistory?.Name} {item.Name}";
            Episodes.ToList().ForEach(episode => episode.Watched = episode.Name == item.Name);
            SaveViewHistory();
        });
    }

    private void PlayerFileLoaded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post((() =>
        {
            _loadingWaitViewModel.Close();

            Status = PlaybackStatus.Playing;
            Duration = TimeSpan.FromSeconds(Mpv!.Duration.Get()!.Value);

            IsMediaLoaded = true;
            if (Duration > TimeSpan.FromSeconds(1))
            {
                lock (Mpv!)
                {
                    Mpv.TimePos.Set(ViewHistory?.PlaybackPosition ?? 0);
                }

                SaveViewHistory();
            }
            else
            {
                SetPositionNoSeek(TimeSpan.Zero);
            }
        }));
    }

    private void PlayerEndFile(object? sender, MpvEndFileEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Reason == MpvEndFileReason.Error)
            {
                Status = PlaybackStatus.Error;
            }

            Stop();

            MediaPlayerOnEndReached();
        });
    }

    /// MPV播放刷新进度条
    private void PlayerPositionChanged(object? sender, MpvValueChangedEventArgs<double, double> e)
    {
        // SetPositionNoSeek(TimeSpan.FromSeconds(e.NewValue!.Value));
        Dispatcher.UIThread.Post(() => SetPositionNoSeek(TimeSpan.FromSeconds(e.NewValue!.Value)));
    }


    /// <summary>
    /// Sets the position without raising PositionChanged.
    /// </summary>
    /// <param name="pos">The position value to set.</param>
    private void SetPositionNoSeek(TimeSpan pos)
    {
        if (!IsSeekBarPressed)
        {
            _isSettingPosition = true; //不要更新播放进度
            Position = pos;
            _isSettingPosition = false; //更新播放进度
        }
    }

    partial void OnMediaUrlChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            // bug：会触发数据库重复添加
            // Stop();
            // Title = "无";
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(async () => { await PlayPause(); });
        }
    }

    partial void OnPositionChanged(TimeSpan value)
    {
        if (IsSeekBarPressed && IsMediaLoaded && !_isSettingPosition)
        {
            lock (Mpv!)
            {
                var pos = TimeSpan.FromTicks(Math.Max(0, Math.Min(Duration.Ticks, value.Ticks)));
                Mpv.TimePos.Set(pos.TotalSeconds);
            }
        }
    }

    partial void OnVolumeChanged(int value)
    {
        Mpv?.Volume.Set(value);
        SaveVolume();
    }

    partial void OnSpeedChanged(double value)
    {
        Mpv?.Speed.Set(value);
    }

    partial void OnLoopChanged(bool value)
    {
        Mpv?.LoopFile.Set(value ? "yes" : "no");
    }

    partial void OnIsMutedChanged(bool value)
    {
        Mpv.Mute.Set(value);
    }

    partial void OnIsVideosKanbanCheckedChanged(bool value)
    {
        KanBanWidth = value ? 300 : 0;
    }

    public async Task Loading()
    {
        var options = new DialogOptions
        {
            Title = "",
            Mode = DialogMode.None,
            Button = DialogButton.None,
            ShowInTaskBar = false,
            IsCloseButtonVisible = true,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanDragMove = true,
            CanResize = false,
            StyleClass = ""
        };

        _loadingWaitViewModel.TimerStart();

        await Dialog.ShowModal<LoadingWaitView, LoadingWaitViewModel>(_loadingWaitViewModel, Window, options: options);
    }

    private bool _disposed;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">The disposing parameter should be false when called from a finalizer, and true when called from the IDisposable.Dispose method.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resources.
            }

            // Unmanaged resources.
            Mpv?.Dispose();

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}