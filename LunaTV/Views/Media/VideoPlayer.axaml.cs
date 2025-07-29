using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using LunaTV.ViewModels.Media;

namespace LunaTV.Views.Media;

public partial class VideoPlayer : UserControl
{
    VideoPlayerViewModel _viewModel;

    public VideoPlayer()
    {
        InitializeComponent();

        _viewModel = new VideoPlayerViewModel();
        DataContext = _viewModel;
    }

    public void Close()
    {
        _viewModel.Stop();
    }

    private async void VideoViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel.MediaPlayer.IsPlaying)
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
            _viewModel.VideoPath = file[0].Path.LocalPath;
            _viewModel.VideoName = _viewModel.VideoPath.Substring(_viewModel.VideoPath.LastIndexOf('\\'));
        }

        base.OnPointerPressed(e);
    }
}