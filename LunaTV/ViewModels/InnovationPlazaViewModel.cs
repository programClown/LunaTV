using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using Microsoft.Extensions.Hosting.Internal;
using Ursa.Controls;

namespace LunaTV.ViewModels;

public partial class InnovationPlazaViewModel : PageViewModelBase
{
    public override string Title => "自由创作";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorLineIcon", out var value) ? (IconSource)value : null;

    [RelayCommand]
    private void PlayVideo()
    {
        var win = new PlayerWindow();

        win.Show();
    }
}