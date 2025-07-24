using System;
using Avalonia.Controls;
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
        InitializeComponent();
        DataContext = new VideoPlayerViewModel();
    }

    private void VideoViewOnPointerEntered(object sender, PointerEventArgs e)
    {
        ControlsPanel.IsVisible = true;
    }

    private void VideoViewOnPointerExited(object sender, PointerEventArgs e)
    {
        ControlsPanel.IsVisible = false;
    }
    
    public void Close()
    {
        var vm = DataContext as VideoPlayerViewModel;
        vm?.Stop();
        // vm?.Dispose();
    }

    private async void VideoViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
            FilePickerFileType VideoAll = new FilePickerFileType("All Videos")
        {
            Patterns = new string[6] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv" },
            AppleUniformTypeIdentifiers = new string[1] { "public.video" },
            MimeTypes = new string[1] { "video/*" }
        };
    
        var file= await App.StorageProvider?.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "打开文件",
            FileTypeFilter = new []
            {
                FilePickerFileTypes.All,
                VideoAll,
            },
            AllowMultiple = false,
        });
        
        if (file.Count > 0)
        {
            var vm = DataContext as VideoPlayerViewModel;
            vm.VideoPath = file[0].Path.LocalPath;
        }
    }
}