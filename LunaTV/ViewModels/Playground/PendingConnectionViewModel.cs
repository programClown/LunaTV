using System.Windows.Input;
using Nodify;

namespace LunaTV.ViewModels.Playground;

public class PendingConnectionViewModel
{
    private ConnectorViewModel _source;

    public PendingConnectionViewModel(PlaygroundViewModel editor)
    {
        StartCommand = new DelegateCommand<ConnectorViewModel>(source => _source = source);
        FinishCommand = new DelegateCommand<ConnectorViewModel>(target =>
        {
            if (target != null)
                editor.Connect(_source, target);
        });
    }

    public ICommand StartCommand { get; }
    public ICommand FinishCommand { get; }
}