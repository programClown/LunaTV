using System;
using System.Diagnostics;
using Avalonia.Controls;

namespace LunaTV.Views;

public static class CompositionTargetEx
{
    private static readonly Stopwatch sw = new();
    private static event Action<double>? FrameUpdating;

    public static event Action<double> Rendering
    {
        add
        {
            if (FrameUpdating == null) sw.Start();
            FrameUpdating += value;
        }
        remove
        {
            FrameUpdating -= value;
            if (FrameUpdating == null) sw.Stop();
        }
    }

    public static void OnRendering(object? sender, EventArgs e)
    {
        var took = sw.Elapsed;
        sw.Restart();

        var fps = 1000 / took.TotalMilliseconds;
        FrameUpdating?.Invoke(fps);
    }
}

public partial class PlaygroundView : UserControl
{
    public PlaygroundView()
    {
        InitializeComponent();
    }
}