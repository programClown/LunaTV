using System;
using Avalonia.Controls;
using Avalonia.Input;
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
}