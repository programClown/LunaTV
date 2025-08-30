using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Media;
using LunaTV.Views;
using Microsoft.Extensions.Hosting.Internal;
using Ursa.Controls;

namespace LunaTV.ViewModels;

public partial class InnovationPlazaViewModel : PageViewModelBase
{
    public override string Title => "自由创作";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorLineIcon", out var value) ? (IconSource)value : null;
    
}