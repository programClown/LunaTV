using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LunaTV.ViewModels.Playground;

public partial class ConnectorViewModel : ObservableObject
{
    [ObservableProperty] private Point _anchor;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _title;
}