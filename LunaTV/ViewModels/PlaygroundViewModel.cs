using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.Base.Web;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using NodifyM.Avalonia.ViewModelBase;

namespace LunaTV.ViewModels;

public partial class PlaygroundViewModel : PageViewModelBase
{
    public override string Title => "创作广场";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorFillIcon", out var value) ? (IconSource)value : null;

    private readonly LunaHttpStaticPageServer _htmlServerProxy;
    [ObservableProperty] private bool _showGridLines;
    [ObservableProperty] private Size _viewportSize;
    [ObservableProperty] private double _zoom = 1.0;

    public PlaygroundNodeViewModel PlaygroundNodeViewModel { get; set; }

    public PlaygroundViewModel()
    {
        _htmlServerProxy = new LunaHttpStaticPageServer();
        _htmlServerProxy?.Start(GlobalDefine.RootPath + "wwwroot/unfake", 8080);

        PlaygroundNodeViewModel = new PlaygroundNodeViewModel();
    }


    [RelayCommand]
    private void ImageNode()
    {
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
}

public partial class PlaygroundNodeViewModel : NodifyEditorViewModelBase
{
    public PlaygroundNodeViewModel()
    {
        this.PendingConnection = new PendingConnectionViewModelBase(this);

        var knot1 = new KnotNodeViewModel()
        {
            Location = new Point(300, 100)
        };
        var input1 = new ConnectorViewModelBase()
        {
            Title = "AS 1",
            Flow = ConnectorViewModelBase.ConnectorFlow.Input
        };
        var output1 = new ConnectorViewModelBase()
        {
            Title = "B 1",
            Flow = ConnectorViewModelBase.ConnectorFlow.Output
        };
        Connections.Add(new ConnectionViewModelBase(this, output1, knot1.Connector, "Test"));
        Connections.Add(new ConnectionViewModelBase(this, knot1.Connector, input1));
        Nodes = new()
        {
            new NodeViewModelBase()
            {
                Location = new Point(400, 2000),
                Title = "Node 1",
                Input = new ObservableCollection<object>
                {
                    input1,
                    new ComboBox()
                    {
                        ItemsSource = new ObservableCollection<object>
                        {
                            "Item 1",
                            "Item 2",
                            "Item 3"
                        }
                    }
                },
                Output = new ObservableCollection<object>
                {
                    new ConnectorViewModelBase()
                    {
                        Title = "Output 2",
                        Flow = ConnectorViewModelBase.ConnectorFlow.Output
                    }
                }
            },
            new NodeViewModelBase()
            {
                Title = "Node 2",
                Location = new Point(-100, -100),
                Input = new ObservableCollection<object>
                {
                    new ConnectorViewModelBase()
                    {
                        Title = "Input 1",
                        Flow = ConnectorViewModelBase.ConnectorFlow.Input
                    },
                    new ConnectorViewModelBase()
                    {
                        Flow = ConnectorViewModelBase.ConnectorFlow.Input,
                        Title = "Input 2"
                    }
                },
                Output = new ObservableCollection<object>
                {
                    output1,
                    new ConnectorViewModelBase()
                    {
                        Flow = ConnectorViewModelBase.ConnectorFlow.Output,
                        Title = "Output 1"
                    },
                    new ConnectorViewModelBase()
                    {
                        Flow = ConnectorViewModelBase.ConnectorFlow.Output,
                        Title = "Output 2"
                    }
                }
            }
        };
        Nodes.Add(knot1);
        knot1.Connector.IsConnected = true;
        output1.IsConnected = true;
        input1.IsConnected = true;
    }

    public override void Connect(ConnectorViewModelBase source, ConnectorViewModelBase target)
    {
        base.Connect(source, target);
    }

    public override void DisconnectConnector(ConnectorViewModelBase connector)
    {
        base.DisconnectConnector(connector);
    }
}