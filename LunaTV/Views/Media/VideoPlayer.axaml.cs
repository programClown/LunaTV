using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LunaTV.ViewModels.Media;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace LunaTV.Views.Media;

public partial class VideoPlayer : UserControl
{
    VideoPlayerViewModel _viewModel;
    private DispatcherTimer _overlayTimer;

    public VideoPlayer()
    {
        InitializeComponent();

        _viewModel = new VideoPlayerViewModel();
        DataContext = _viewModel;
        _overlayTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _overlayTimer.Tick += (s, e) =>
        {
            _overlayTimer.Stop();
            ControlsPanel.IsVisible = false;
        };
    }

    public void Close()
    {
        _overlayTimer.Stop();
        _viewModel.Stop();
    }

    private async void VideoViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var videoAll = new FilePickerFileType("All Videos")
        {
            Patterns = new string[6] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv" },
            AppleUniformTypeIdentifiers = new string[1] { "public.video" },
            MimeTypes = new string[1] { "video/*" }
        };


        var file = await (Parent as Window)?.StorageProvider?.OpenFilePickerAsync(
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
            _viewModel.VideoPath = file[0].Path.LocalPath;
            _viewModel.VideoName = file[0].Path.LocalPath.Substring(file[0].Path.LocalPath.LastIndexOf('\\') + 1);
        }

        base.OnPointerPressed(e);
    }

    private void VideoViewOnPointerEntered(object? sender, PointerEventArgs e)
    {
        _overlayTimer.Stop();
        _overlayTimer.Start();
        ControlsPanel.IsVisible = true;
    }
}