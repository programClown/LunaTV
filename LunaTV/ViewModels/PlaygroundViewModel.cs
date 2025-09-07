using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.Base.Web;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Playground;

namespace LunaTV.ViewModels;

public partial class PlaygroundViewModel : PageViewModelBase
{
    private readonly LunaHttpStaticPageServer _htmlServerProxy;
    [ObservableProperty] private ObservableCollection<ConnectionViewModel> _connections = new();
    [ObservableProperty] private ObservableCollection<NodeViewModel> _nodes = new();
    [ObservableProperty] private bool _showGridLines;
    [ObservableProperty] private Size _viewportSize;
    [ObservableProperty] private double _zoom = 1.0;

    public PlaygroundViewModel()
    {
        _htmlServerProxy = new LunaHttpStaticPageServer();
        _htmlServerProxy?.Start(GlobalDefine.RootPath + "wwwroot/unfake", 8080);

        PendingConnection = new PendingConnectionViewModel(this);
    }

    public PendingConnectionViewModel PendingConnection { get; }

    public override string Title => "创作广场";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorFillIcon", out var value) ? (IconSource)value : null;

    [RelayCommand]
    private void ImageNode()
    {
        Nodes.Add(new NodeViewModel
        {
            Title = "节点1",
            Location = new Point(10, 10),
            Input =
            {
                new ConnectorViewModel
                {
                    Title = "In 1"
                }
            },
            Output =
            {
                new ConnectorViewModel
                {
                    Title = "Out 1"
                }
            }
        });
        Nodes.Add(new NodeViewModel
        {
            Title = "节点2",
            Location = new Point(10, 10),
            Input =
            {
                new ConnectorViewModel
                {
                    Title = "In 2"
                }
            },
            Output =
            {
                new ConnectorViewModel
                {
                    Title = "Out 2"
                }
            }
        });
        Connections.Add(new ConnectionViewModel
            (Nodes[0].Output[0],
                Nodes[1].Input[0])
        );
    }


    [RelayCommand]
    private async void AlgorithmNode()
    {
        await App.TopLevel.Launcher.LaunchUriAsync(new Uri("http://localhost:8080/index.html"));
    }

    [RelayCommand]
    private void DisplayNode()
    {
    }

    public void Connect(ConnectorViewModel source, ConnectorViewModel target)
    {
        Connections.Add(new ConnectionViewModel(source, target));
    }
}