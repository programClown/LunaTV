using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace LunaTV.Views;

public partial class PlaygroundView : UserControl
{
    private DispatcherTimer _timer;
    private int _index = 0;

    public PlaygroundView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _timer.Tick += (_, _) =>
        {
            Carousel.SelectedIndex = _index % 3;
            _index++;
        };
        _timer.Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _timer.Stop();
    }
}