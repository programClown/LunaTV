using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using Nodify.Playground;

namespace LunaTV.ViewModels;

public partial class PlaygroundViewModel : PageViewModelBase
{
    [ObservableProperty] private ObservableCollection<ConnectionViewModel> _connections = new();
    [ObservableProperty] private PointEditor _location;
    [ObservableProperty] private PointEditor _minimapViewportOffset;
    [ObservableProperty] private ObservableCollection<NodeViewModel> _nodes = new();
    [ObservableProperty] private PendingConnectionViewModel? _pendingConnection;
    [ObservableProperty] private bool _resizeToViewport;
    [ObservableProperty] private ConnectionViewModel? _selectedConnection;
    [ObservableProperty] private ObservableCollection<ConnectionViewModel> _selectedConnections = new();
    [ObservableProperty] private NodeViewModel? _selectedNode;
    [ObservableProperty] private ObservableCollection<NodeViewModel> _selectedNodes = new();
    [ObservableProperty] private Size _viewportSize;
    [ObservableProperty] private double _zoom = 1.0;

    public override string Title => "创作广场";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorFillIcon", out var value) ? (IconSource)value : null;

    [RelayCommand]
    private void ImageNode()
    {
        var node = new FlowNodeViewModel
        {
            Title = "Node 1",
            Location = new Point(200, 300)
        };
        Nodes.Add(node);
        node.Input.Add(new ConnectorViewModel
        {
            Title = "NEW 1",
            Shape = ConnectorShape.Circle
        });
        node.Input.Add(new ConnectorViewModel
        {
            Title = "NEW 2",
            Shape = ConnectorShape.Square
        });
    }


    [RelayCommand]
    private void AlgorithmNode()
    {
    }

    [RelayCommand]
    private void DisplayNode()
    {
    }
}