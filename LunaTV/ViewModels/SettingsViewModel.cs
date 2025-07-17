using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels;

public class SettingsViewModel : PageViewModelBase
{
    public override string Title => "设置";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("SettingsIcon", out var value) ? (IconSource)value : null;
}