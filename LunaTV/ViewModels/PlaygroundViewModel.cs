using System;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using Nodify;
using Nodify.Playground;

namespace LunaTV.ViewModels;

public class PlaygroundViewModel : PageViewModelBase
{
    public PlaygroundViewModel()
    {
        GenerateRandomNodesCommand = new DelegateCommand(GenerateRandomNodes);
        PerformanceTestCommand = new DelegateCommand(PerformanceTest);
        ToggleConnectionsCommand = new DelegateCommand(ToggleConnections);
        ResetCommand = new DelegateCommand(ResetGraph);

        //BindingOperations.EnableCollectionSynchronization(GraphViewModel.Nodes, GraphViewModel.Nodes);
        //BindingOperations.EnableCollectionSynchronization(GraphViewModel.Connections, GraphViewModel.Connections);

        Settings.PropertyChanged += OnSettingsChanged;
    }

    public NodifyEditorViewModel GraphViewModel { get; } = new();

    public override string Title => "创作广场";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorFillIcon", out var value) ? (IconSource)value : null;

    public ICommand GenerateRandomNodesCommand { get; }
    public ICommand PerformanceTestCommand { get; }
    public ICommand ToggleConnectionsCommand { get; }
    public ICommand ResetCommand { get; }
    public PlaygroundSettings Settings => PlaygroundSettings.Instance;

    public string ConnectNodesText => Settings.ShouldConnectNodes ? "CONNECT NODES" : "DISCONNECT NODES";

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaygroundSettings.ShouldConnectNodes))
            OnPropertyChanged(nameof(ConnectNodesText));
    }

    private void ResetGraph()
    {
        GraphViewModel.Nodes.Clear();
        EditorSettings.Instance.Location = new Point(0, 0);
        EditorSettings.Instance.Zoom = 1.0d;
    }

    private async void GenerateRandomNodes()
    {
        var minNodesByType = Settings.MinNodes / 5;
        var maxNodesByType = Settings.MaxNodes / 20;

        var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
        {
            MinNodesCount = minNodesByType,
            MaxNodesCount = maxNodesByType,
            MinInputCount = Settings.MinConnectors,
            MaxInputCount = Settings.MaxConnectors,
            MinOutputCount = Settings.MinConnectors,
            MaxOutputCount = Settings.MaxConnectors,
            GridSnap = EditorSettings.Instance.GridSpacing
        });

        var verticalNodes = RandomNodesGenerator.GenerateNodes<VerticalNodeViewModel>(
            new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

        GraphViewModel.Nodes.Clear();
        await CopyToAsync(nodes, GraphViewModel.Nodes);
        //await CopyToAsync(verticalNodes, GraphViewModel.Nodes);

        if (Settings.ShouldConnectNodes) await ConnectNodes();
    }

    private async void ToggleConnections()
    {
        if (Settings.ShouldConnectNodes)
            await ConnectNodes();
        else
            GraphViewModel.Connections.Clear();
    }

    private async void PerformanceTest()
    {
        var count = Settings.PerformanceTestNodes;
        var distance = 500;
        var size = (int)count / (int)Math.Sqrt(count);

        var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(count)
        {
            NodeLocationGenerator = (s, i) => new Point(i % size * distance, i / size * distance),
            MinInputCount = Settings.MinConnectors,
            MaxInputCount = Settings.MaxConnectors,
            MinOutputCount = Settings.MinConnectors,
            MaxOutputCount = Settings.MaxConnectors,
            GridSnap = EditorSettings.Instance.GridSpacing
        });

        GraphViewModel.Nodes.Clear();
        await CopyToAsync(nodes, GraphViewModel.Nodes);

        if (Settings.ShouldConnectNodes) await ConnectNodes();
    }

    private async Task ConnectNodes()
    {
        var schema = new GraphSchema();
        var connections = RandomNodesGenerator.GenerateConnections(GraphViewModel.Nodes);

        if (Settings.AsyncLoading)
            await Task.Run(() =>
            {
                for (var i = 0; i < connections.Count; i++)
                {
                    var con = connections[i];
                    schema.TryAddConnection(con.Input, con.Output);
                }
            });
        else
            for (var i = 0; i < connections.Count; i++)
            {
                var con = connections[i];
                schema.TryAddConnection(con.Input, con.Output);
            }
    }

    private async Task CopyToAsync(IList source, IList target)
    {
        if (Settings.AsyncLoading)
            await Task.Run(() =>
            {
                for (var i = 0; i < source.Count; i++) target.Add(source[i]);
            });
        else
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
    }
}