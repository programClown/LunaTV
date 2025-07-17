using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels;

public class TVShowViewModel : PageViewModelBase
{
    public override string Title => "无限影视";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("VideoIcon", out var value) ? (IconSource)value : null;
}