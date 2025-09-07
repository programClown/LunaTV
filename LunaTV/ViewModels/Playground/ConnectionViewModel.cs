using CommunityToolkit.Mvvm.ComponentModel;

namespace LunaTV.ViewModels.Playground;

public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty] private ConnectorViewModel _source;
    [ObservableProperty] private ConnectorViewModel _target;

    public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
    {
        Source = source;
        Target = target;
        Source.IsConnected = true;
        Target.IsConnected = true;
    }
}