using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels;

public class InnovationPlazaViewModel : PageViewModelBase
{
    public override string Title => "自由创作";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorLineIcon", out var value) ? (IconSource)value : null;
}