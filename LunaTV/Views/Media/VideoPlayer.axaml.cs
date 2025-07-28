using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LunaTV.ViewModels.Media;
using Ursa.Controls;

namespace LunaTV.Views.Media;

public partial class VideoPlayer : UserControl
{
    public VideoPlayer()
    {
        DataContext = new VideoPlayerViewModel();
        InitializeComponent();
    }

    public void Close()
    {
        var vm = DataContext as VideoPlayerViewModel;
        vm?.Stop();
    }

    private async void VideoViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = DataContext as VideoPlayerViewModel;
        if (vm.MediaPlayer.IsPlaying)
        {
            ControlsPanel.IsVisible = !ControlsPanel.IsVisible;
            base.OnPointerPressed(e);
            return;
        }

        var videoAll = new FilePickerFileType("All Videos")
        {
            Patterns = new string[6] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv" },
            AppleUniformTypeIdentifiers = new string[1] { "public.video" },
            MimeTypes = new string[1] { "video/*" }
        };


        var file = await (this.Parent as Window)?.StorageProvider?.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "打开文件",
            FileTypeFilter = new[]
            {
                videoAll,
                FilePickerFileTypes.All,
            },
            AllowMultiple = false,
        });

        if (file.Count > 0)
        {
            vm.VideoPath = file[0].Path.LocalPath;
            vm.VideoName = vm.VideoPath.Substring(vm.VideoPath.LastIndexOf('\\'));
        }

        base.OnPointerPressed(e);
    }
}