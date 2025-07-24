using System;
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

    [RelayCommand]
    private void Play()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        if (!IsPlay)
        {
            using var media = new LibVLCSharp.Shared.Media(_libVlc,
                new Uri("D:\\\\迅雷下载\\\\[电影天堂www.dytt89.com]第九区BD国英双语双字.mkv"));
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

    private void Stop()
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
}