using System;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LibVLCSharp.Shared;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.Media;

public partial class VideoPlayerViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc = new LibVLC();

    public MediaPlayer MediaPlayer { get; }

    public string VideoName { set; get; } = "xxxxxx";

    public VideoPlayerViewModel()
    {
        MediaPlayer = new MediaPlayer(_libVlc);
    }

    public bool IsPlay { get; set; }
    public bool IsMuted { get; set; }

    [ObservableProperty] private Symbol _playIcon = Symbol.PlayFilled;
    [ObservableProperty] private double _seekPosition = 0;
    [ObservableProperty] private bool _canInteractSeekSlider = true;
    [ObservableProperty] private double _maximumSeekValue;

    [ObservableProperty] private Symbol _muteIcon = Symbol.Speaker2Filled;
    [ObservableProperty] private float _volume;
    
    [ObservableProperty] private string _videoPath;
    
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
            using var media = new LibVLCSharp.Shared.Media(_libVlc,
                VideoPath);
            MediaPlayer.Play(media);
            PlayIcon = Symbol.PauseFilled;
        }
        else
        {
            PlayIcon = Symbol.PlayFilled;
            Stop();
        }

        IsPlay = !IsPlay;
    }

    public void Stop()
    {
        MediaPlayer.Stop();
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
    }
    
    partial void OnVideoPathChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        VideoPath = value;
        VideoName = value.Split(".").LastOrDefault();
        Play();
    }
}