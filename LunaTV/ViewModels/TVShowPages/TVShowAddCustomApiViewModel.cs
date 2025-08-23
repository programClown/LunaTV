using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowAddCustomApiViewModel : ViewModelBase
{
    [ObservableProperty] private string? _apiName;
    [ObservableProperty] private string _apiSource;
    [ObservableProperty] private string _apiBaseUrl;
    [ObservableProperty] private string _detailBaseUrl;
    [ObservableProperty] private bool _isAdult;
}