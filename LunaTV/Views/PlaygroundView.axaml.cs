using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Nodify;
using Nodify.Playground;

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
    private readonly Random _rand = new();

    private CancellationTokenSource? _animationTokenSource;

    public PlaygroundView()
    {
        InitializeComponent();
        CompositionTargetEx.Rendering += OnRendering;
    }

    private void OnRendering(double fps)
    {
        Dispatcher.UIThread.Post(() => { FPSText.Text = fps.ToString("###"); });
    }

    private void BringIntoView_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaygroundViewModel model)
        {
            var nodes = model.GraphViewModel.Nodes;
            var index = _rand.Next(nodes.Count);

            if (nodes.Count > index)
            {
                var node = nodes[index];
                EditorCommands.BringIntoView.Execute(node.Location, EditorView.EditorInstance);
            }
        }
    }

    private void AnimateConnections_Click(object sender, RoutedEventArgs e)
    {
        EditorSettings.Instance.IsAnimatingConnections = !EditorSettings.Instance.IsAnimatingConnections;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        CompositionTargetEx.OnRendering(null, default!);
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Send);
    }
}