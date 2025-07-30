using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels;

public class PlaygroundViewModel : PageViewModelBase
{
    public override string Title => "创作广场";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorFillIcon", out var value) ? (IconSource)value : null;
}