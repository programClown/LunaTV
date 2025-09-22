using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.LibMpv;
using HanumanInstitute.LibMpv.Avalonia;
using HanumanInstitute.LibMpv.Core;
using LunaTV.Utils;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Media;
using LunaTV.Views;
using LunaTV.Views.Media;

namespace LunaTV.ViewModels;

public partial class MpvPlayerWindowModel : ViewModelBase, IDisposable
{
    [ObservableProperty] private string _mediaUrl = "https://vip.dytt-luck.com/20250827/19457_e0c4ac2b/index.m3u8";
    [ObservableProperty] private int _volume = 100;
    [ObservableProperty] private double _speed = 1.0f; //0.5,1.0,1.5,2.0,2.5,3.0,3.5,4.0
    [ObservableProperty] private bool _loop; //循环播放
    [ObservableProperty] private bool _isMediaLoaded;
    [ObservableProperty] private TimeSpan _position;
    [ObservableProperty] private TimeSpan _duration = TimeSpan.FromSeconds(1);
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private string? _title;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private TimeSpan _positionBar = TimeSpan.Zero;
    public MpvContext Mpv { get; set; } = default!;

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

    public async Task OnWindowLoaded()
    {
        await Task.Delay(100); // Fails to load if we don't give a slight delay.

        Mpv.FileLoaded += PlayerFileLoaded;
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
            _status = value;
            var text = _status switch
            {
                PlaybackStatus.Loading => "Loading...",
                PlaybackStatus.Playing => Path.GetFileName(MediaUrl),
                PlaybackStatus.Error => "Error loading media",
                _ => ""
            };

            App.Notification?.Show(new Notification("播放信息", text, NotificationType.Information),
                NotificationType.Information);
        }
    }

    [RelayCommand]
    private async Task PlayPause()
    {
        if (string.IsNullOrEmpty(MediaUrl) || Design.IsDesignMode)
        {
            return;
        }

        if (!_isLoaded)
        {
            await Mpv.Stop().InvokeAsync();
            await Mpv.Pause.SetAsync(false);
            if (!string.IsNullOrEmpty(MediaUrl))
            {
                await Mpv.LoadFile(MediaUrl!).InvokeAsync();
                IsPlaying = true;
                _isLoaded = true;
            }
            else
            {
                IsPlaying = false;
            }
        }
        else
        {
            await Mpv.Pause.SetAsync(IsPlaying);
            IsPlaying = !IsPlaying;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        Mpv.Stop().Invoke();
        Mpv.Pause.Set(false);

        MediaUrl = string.Empty;
        _isLoaded = false;
        IsPlaying = false;
        Status = PlaybackStatus.Stopped;
    }

    [RelayCommand]
    private void Seek(int seconds)
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
            PositionBar = newPos;
            Position = newPos;
        }
    }

    [RelayCommand]
    private async Task GoHead()
    {
        // if (!IsMediaLoaded)
        // {
        //     return;
        // }
        //
        // Position = TimeSpan.Zero;
        var videoAll = new FilePickerFileType("All Videos")
        {
            Patterns = new string[6] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv" },
            AppleUniformTypeIdentifiers = new string[1] { "public.video" },
            MimeTypes = new string[1] { "video/*" }
        };


        var file = await App.StorageProvider?.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "打开文件",
                FileTypeFilter = new[]
                {
                    videoAll,
                    FilePickerFileTypes.All,
                },
                AllowMultiple = false,
            });

        if (file is { Count: > 0 })
        {
            MediaUrl = file[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private void GoTail()
    {
        if (!IsMediaLoaded)
        {
            return;
        }

        Position = Duration;
    }

    /// <summary>
    /// Restarts playback.
    /// </summary>
    public virtual void Restart()
    {
    }

    private void PlayerFileLoaded(object? sender, EventArgs e)
    {
        Status = PlaybackStatus.Playing;
        Duration = TimeSpan.FromSeconds(Mpv!.Duration.Get()!.Value);

        SetPositionNoSeek(TimeSpan.Zero);
        IsMediaLoaded = true;
    }

    private void PlayerEndFile(object? sender, MpvEndFileEventArgs e)
    {
        if (e.Reason == MpvEndFileReason.Error)
        {
            Status = PlaybackStatus.Error;
        }

        IsMediaLoaded = false;
        Duration = TimeSpan.FromSeconds(1);
        IsPlaying = false;
    }

    private void PlayerPositionChanged(object? sender, MpvValueChangedEventArgs<double, double> e)
    {
        SetPositionNoSeek(TimeSpan.FromSeconds(e.NewValue!.Value));
    }

    /// <summary>
    /// Sets the position without raising PositionChanged.
    /// </summary>
    /// <param name="pos">The position value to set.</param>
    private void SetPositionNoSeek(TimeSpan pos)
    {
        lock (Mpv)
        {
            _isSettingPosition = true;
            Position = pos;
            _isSettingPosition = false;
        }
    }

    private void PlayerPositionChanged(TimeSpan value)
    {
        if (!IsSeekBarPressed && IsMediaLoaded)
        {
            PositionBar = Position;
        }
    }

    partial void OnPositionChanged(TimeSpan value)
    {
        var pos = TimeSpan.FromTicks(Math.Max(0, Math.Min(Duration.Ticks, value.Ticks)));
        lock (Mpv!)
        {
            if (IsMediaLoaded && _isSettingPosition)
            {
                Mpv.TimePos.Set(pos.TotalSeconds);
            }
        }
    }

    partial void OnVolumeChanged(int value)
    {
        Mpv?.Volume.Set(value);
    }

    partial void OnSpeedChanged(double value)
    {
        Mpv?.Speed.Set(value);
    }

    partial void OnLoopChanged(bool value)
    {
        Mpv?.LoopFile.Set(value ? "yes" : "no");
    }

    partial void OnPositionBarChanged(TimeSpan value)
    {
        if (IsSeekBarPressed)
        {
            Position = value;
        }
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