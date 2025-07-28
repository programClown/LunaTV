using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LibVLCSharp.Shared;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.Media;

public partial class VideoPlayerViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc = new();

    public MediaPlayer MediaPlayer { get; }

    public VideoPlayerViewModel()
    {
        MediaPlayer = new MediaPlayer(_libVlc);
        MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
    }

    public bool IsPlay { get; set; }
    public bool IsMuted { get; set; }

    [ObservableProperty] private string _videoName = "xxxxxx";
    [ObservableProperty] private Symbol _playIcon = Symbol.PlayFilled;
    [ObservableProperty] private double _seekPosition = 0;
    [ObservableProperty] private bool _canInteractSeekSlider = true;
    [ObservableProperty] private double _maximumSeekValue = 0;

    [ObservableProperty] private Symbol _muteIcon = Symbol.Speaker2Filled;
    [ObservableProperty] private float _volume = 0.5f;

    [ObservableProperty] private string _videoPath;

    [RelayCommand]
    private async Task Play()
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
                using var media = new LibVLCSharp.Shared.Media(_libVlc,
                    VideoPath);

                await media.Parse();

                if (media.IsParsed)
                {
                    await Task.Delay(300);
                }

                MaximumSeekValue = media.Duration / 1000.0;

                MediaPlayer.Play(media);
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

    public void Stop()
    {
        IsPlay = false;
        MediaPlayer.Stop();
    }

    public void Pause()
    {
        MediaPlayer.Pause();
    }

    public void Dispose()
    {
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
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

        VideoPath = value;
        VideoName = value.Substring(value.LastIndexOf('\\') + 1);
        Task.Run(async () => await Play());
    }

    partial void OnSeekPositionChanged(double value)
    {
        // if (MediaPlayer.IsSeekable)
        // {
        //     MediaPlayer.PositionChanged -= MediaPlayerOnPositionChanged;
        //     MediaPlayer.SeekTo(TimeSpan.FromSeconds(value));
        //     MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
        // }
    }

    private void MediaPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        SeekPosition = e.Position * MaximumSeekValue;
    }


    partial void OnVolumeChanged(float value)
    {
        if (MediaPlayer.IsSeekable)
        {
            MediaPlayer.Volume = (int)(value * 100);
        }
    }
}