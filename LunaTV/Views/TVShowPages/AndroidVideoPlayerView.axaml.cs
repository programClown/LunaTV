#if ANDROID
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HanumanInstitute.LibMpv;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.Views.TVShowPages;

public partial class AndroidVideoPlayerView : UserControl
{
    private MpvContext? _mpv;
    private DispatcherTimer? _progressTimer;
    private DispatcherTimer? _hideControlsTimer;
    private bool _isSeeking;
    private bool _isPlaying;
    private double _duration;
    private ViewHistory? _viewHistory;
    private Action? _onClose;

    // Speed control
    private static readonly double[] _speeds = { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };
    private int _currentSpeedIndex = 2; // Default 1.0x

    // Volume control
    private double _currentVolume = 100;

    public AndroidVideoPlayerView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        BackButton.Click += OnBackClick;
        PlayPauseButton.Click += OnPlayPauseClick;
        SeekSlider.PropertyChanged += OnSeekSliderChanged;
        SpeedButton.Click += OnSpeedClick;
        VolumeDownButton.Click += OnVolumeDownClick;
        VolumeUpButton.Click += OnVolumeUpClick;

        // Tap anywhere on the video to toggle controls
        PointerPressed += OnVideoTapped;

        // Progress timer
        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _progressTimer.Tick += OnProgressTick;
        _progressTimer.Start();

        // Auto-hide controls timer
        _hideControlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _hideControlsTimer.Tick += (_, _) =>
        {
            if (_isPlaying) ControlsOverlay.IsVisible = false;
            _hideControlsTimer.Stop();
        };
        _hideControlsTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _progressTimer?.Stop();
        _hideControlsTimer?.Stop();
        SaveHistory();
        _mpv?.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Start playback with the given parameters.
    /// </summary>
    public async Task PlayAsync(string mediaUrl, string title, ViewHistory? viewHistory, Action? onClose)
    {
        _viewHistory = viewHistory;
        _onClose = onClose;
        TitleText.Text = title;

        // Wait for MpvView to initialize and expose its MpvContext
        await Task.Delay(200); // Allow NativeView surface creation

        _mpv = MpvVideoView.MpvContext;
        if (_mpv == null) return;

        // Configure network playback
        if (mediaUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            _mpv.SetOptionString("user-agent",
                "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36");
            try
            {
                var uri = new Uri(mediaUrl);
                _mpv.SetOptionString("referrer", $"{uri.Scheme}://{uri.Authority}");
            }
            catch { /* ignore malformed URIs */ }

            _mpv.SetOptionString("network-timeout", "15");
            _mpv.SetOptionString("cache", "yes");
            _mpv.SetOptionString("cache-pause", "yes");
            _mpv.SetOptionString("cache-pause-initial", "yes");
            _mpv.SetOptionString("cache-pause-wait", "2");
            _mpv.SetOptionString("cache-secs", "180");
            _mpv.SetOptionString("demuxer-readahead-secs", "60");
            _mpv.SetOptionString("demuxer-max-bytes", "256MiB");
            _mpv.SetOptionString("demuxer-max-back-bytes", "64MiB");
        }

        // Resume from saved position
        if (viewHistory is { PlaybackPosition: > 0 })
        {
            _mpv.SetOptionString("start", $"+{viewHistory.PlaybackPosition:F1}");
        }

        _mpv.SetOptionString("keep-open", "always");
        _mpv.SetOptionString("sid", "no");
        _mpv.SetOptionString("hr-seek", "yes");

        // Subscribe to events
        _mpv.FileLoaded += OnFileLoaded;
        _mpv.EndFile += OnEndFile;

        // Load and play
        _mpv.LoadFile(mediaUrl);
        _isPlaying = true;
        PlayPauseButton.Content = "⏸";
    }

    private void OnFileLoaded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _duration = _mpv?.Duration.Get() ?? 0;
            SeekSlider.Maximum = _duration;
            DurationText.Text = FormatTime(_duration);
        });
    }

    private void OnEndFile(object? sender, MpvEndFileEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isPlaying = false;
            PlayPauseButton.Content = "▶";
            ControlsOverlay.IsVisible = true;
        });
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        if (_mpv == null || _isSeeking) return;

        try
        {
            var pos = _mpv.TimePos.Get() ?? 0;
            SeekSlider.Value = pos;
            PositionText.Text = FormatTime(pos);
        }
        catch
        {
            // Mpv may not be ready yet
        }
    }

    private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
    {
        if (_mpv == null) return;

        if (_isPlaying)
        {
            _mpv.SetPropertyString("pause", "yes");
            _isPlaying = false;
            PlayPauseButton.Content = "▶";
        }
        else
        {
            _mpv.SetPropertyString("pause", "no");
            _isPlaying = true;
            PlayPauseButton.Content = "⏸";
            ResetHideTimer();
        }
    }

    private void OnSeekSliderChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != "Value" || _mpv == null) return;
        if (!SeekSlider.IsPointerOver) return; // Only react to user interaction

        var newPos = SeekSlider.Value;
        _mpv.TimePos.Set(newPos);
        PositionText.Text = FormatTime(newPos);
    }

    private void OnVideoTapped(object? sender, PointerPressedEventArgs e)
    {
        ControlsOverlay.IsVisible = !ControlsOverlay.IsVisible;
        if (ControlsOverlay.IsVisible) ResetHideTimer();
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        SaveHistory();
        _mpv?.Stop();
        _onClose?.Invoke();
    }

    private void OnSpeedClick(object? sender, RoutedEventArgs e)
    {
        if (_mpv == null) return;

        _currentSpeedIndex = (_currentSpeedIndex + 1) % _speeds.Length;
        var newSpeed = _speeds[_currentSpeedIndex];
        _mpv.Speed.Set(newSpeed);
        SpeedButton.Content = $"{newSpeed:F2}x";
    }

    private void OnVolumeDownClick(object? sender, RoutedEventArgs e)
    {
        if (_mpv == null) return;
        _currentVolume = Math.Max(0, _currentVolume - 10);
        _mpv.Volume.Set(_currentVolume);
    }

    private void OnVolumeUpClick(object? sender, RoutedEventArgs e)
    {
        if (_mpv == null) return;
        _currentVolume = Math.Min(100, _currentVolume + 10);
        _mpv.Volume.Set(_currentVolume);
    }

    private void SaveHistory()
    {
        if (_viewHistory == null || _mpv == null) return;

        try
        {
            var pos = _mpv.TimePos.Get() ?? 0;
            var dur = _mpv.Duration.Get() ?? 0;
            _viewHistory.PlaybackPosition = (int)pos;
            _viewHistory.Duration = (int)dur;

            var repo = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
            if (_viewHistory.Id > 0)
                repo.Update(_viewHistory);
            else
                repo.Insert(_viewHistory);
        }
        catch
        {
            // Ignore - mpv might already be destroyed
        }
    }

    private void ResetHideTimer()
    {
        _hideControlsTimer?.Stop();
        _hideControlsTimer?.Start();
    }

    private static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0) seconds = 0;
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
#endif
