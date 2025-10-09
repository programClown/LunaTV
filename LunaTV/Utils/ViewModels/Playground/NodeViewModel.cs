using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LunaTV.ViewModels.Playground;

public partial class NodeViewModel : ObservableObject
{
    [ObservableProperty] private Point _location;
    [ObservableProperty] private string _title;

    public ObservableCollection<ConnectorViewModel> Input { get; } = new();
    public ObservableCollection<ConnectorViewModel> Output { get; } = new();
}