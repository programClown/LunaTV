using Avalonia.Controls;
using Avalonia.Input;
using LunaTV.ViewModels.Media;

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
}