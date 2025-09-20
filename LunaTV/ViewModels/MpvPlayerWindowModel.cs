using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.LibMpv;
using HanumanInstitute.LibMpv.Avalonia;
using HanumanInstitute.LibMpv.Core;
using LunaTV.Utils;
using LunaTV.ViewModels.Base;
using LunaTV.Views.Media;

namespace LunaTV.ViewModels;

public partial class MpvPlayerWindowModel : ViewModelBase, IDisposable
{
    [ObservableProperty] private string _mediaUrl = "https://vip.dytt-luck.com/20250827/19457_e0c4ac2b/index.m3u8";
    [ObservableProperty] private VideoRenderer _renderer;
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
    private readonly TimedAction<TimeSpan> _positionBarTimedUpdate;

    /// <summary>
    /// Gets or sets whether the user is dragging the seek bar.
    /// </summary>
    private bool IsSeekBarPressed { get; set; }

    // Restart won't be triggered after Stop while this timer is running.
    private bool _isStopping;
    private DispatcherTimer? _stopTimer;

    public async Task OnWindowLoaded()
    {
        _stopTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
        {
            _stopTimer?.Stop();
            _isStopping = false;
        });

        await Task.Delay(100); // Fails to load if we don't give a slight delay.

        Mpv!.FileLoaded += PlayerFileLoaded;
        Mpv!.EndFile += PlayerEndFile;

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
            await Mpv!.Stop().InvokeAsync();
            await Mpv.Pause.SetAsync(false);
            if (!string.IsNullOrEmpty(MediaUrl))
            {
                _isLoaded = true;
                Thread.Sleep(10);
                await Mpv.Pause.SetAsync(false);
                await Mpv.LoadFile(MediaUrl!).InvokeAsync();
                IsPlaying = true;

                _isStopping = true;
                // Use timer for Loop feature if player doesn't support it natively, but not after pressing Stop.
                _stopTimer?.Stop();
                _stopTimer?.Start();
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

        _isStopping = true;
        // Use timer for Loop feature if player doesn't support it natively, but not after pressing Stop.
        _stopTimer?.Stop();
        _stopTimer?.Start();
        MediaUrl = string.Empty;
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

    /// <summary>
    /// Restarts playback.
    /// </summary>
    public virtual void Restart()
    {
    }

    public void Software()
    {
        Renderer = VideoRenderer.Software;
    }

    public void OpenGl()
    {
        Renderer = VideoRenderer.OpenGl;
    }

    public void Native()
    {
        Renderer = VideoRenderer.Native;
    }

    private void PlayerFileLoaded(object? sender, EventArgs e)
    {
        Status = PlaybackStatus.Playing;
        Duration = TimeSpan.FromSeconds(Mpv!.Duration.Get()!.Value);
        OnMediaLoaded();
    }

    private void PlayerEndFile(object? sender, MpvEndFileEventArgs e)
    {
        if (e.Reason == MpvEndFileReason.Error)
        {
            Status = PlaybackStatus.Error;
        }

        OnMediaUnloaded();
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

    /// <summary>
    /// Must be called by the derived class when media is loaded.
    /// </summary>
    private void OnMediaLoaded()
    {
        SetPositionNoSeek(TimeSpan.Zero);
        IsMediaLoaded = true;

        PlayerMediaLoaded();
    }

    /// <summary>
    /// Must be called by the derived class when media is unloaded.
    /// </summary>
    private void OnMediaUnloaded()
    {
        if (Loop && !_isStopping)
        {
            Restart();
        }
        else
        {
            IsMediaLoaded = false;
            Duration = TimeSpan.FromSeconds(1);
        }

        PlayerMediaUnloaded();
    }

    private void PlayerMediaLoaded()
    {
        PlayerPositionChanged(TimeSpan.Zero);
    }

    private void PlayerMediaUnloaded()
    {
        PositionBar = TimeSpan.Zero;
    }

    private void PlayerPositionChanged(TimeSpan value)
    {
        if (!IsSeekBarPressed && IsMediaLoaded)
        {
            PositionBar = Position;
        }
    }

    private async Task LoadMediaAsync()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        await Mpv!.Stop().InvokeAsync();
        if (!string.IsNullOrEmpty(MediaUrl))
        {
            _isLoaded = true;
            Thread.Sleep(10);
            await Mpv.Pause.SetAsync(false);
            await Mpv.LoadFile(MediaUrl!).InvokeAsync();
        }
    }

    partial void OnMediaUrlChanged(string value)
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        if (!string.IsNullOrEmpty(value))
        {
            Status = PlaybackStatus.Loading;
            _ = LoadMediaAsync();
        }
        else
        {
            Status = PlaybackStatus.Stopped;
            Mpv?.Stop().Invoke();
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
            // _positionBarTimedUpdate.ExecuteAtInterval(PositionBar);
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