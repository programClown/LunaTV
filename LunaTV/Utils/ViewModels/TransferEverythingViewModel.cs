using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels;

public class TransferEverythingViewModel : PageViewModelBase
{
    public override string Title => "万物快传";

    public override IconSource IconSource { get; set; } =
        App.TopLevel.TryFindResource("DirectionsIcon", out var value) ? (IconSource)value : null;
}