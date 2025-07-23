using System;
using Avalonia.Controls;
using LibVLCSharp.Shared;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.Media;

public class VideoPlayerViewModel : ViewModelBase, IDisposable
{
    private readonly LibVLC _libVlc = new LibVLC();

    public MediaPlayer MediaPlayer { get; }

    public VideoPlayerViewModel()
    {
        MediaPlayer = new MediaPlayer(_libVlc);
    }

    public void Play()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        using var media = new LibVLCSharp.Shared.Media(_libVlc,
            new Uri("D:\\\\迅雷下载\\\\[电影天堂www.dytt89.com]第九区BD国英双语双字.mkv"));
        MediaPlayer.Play(media);
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
}