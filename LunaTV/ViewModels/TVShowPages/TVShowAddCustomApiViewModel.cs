using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowAddCustomApiViewModel : ViewModelBase
{
    [ObservableProperty] private string? _apiName;
    [ObservableProperty] private string? _apiSource;
    [ObservableProperty] private string? _apiBaseUrl;
    [ObservableProperty] private string? _detailBaseUrl;
    [ObservableProperty] private bool _isAdult;
    [ObservableProperty] private bool _apiSourceErrorVisible;
    [ObservableProperty] private bool _apiBaseUrlErrorVisible;
    [ObservableProperty] private bool _apiNameErrorVisible;

    partial void OnApiSourceChanged(string? value)
    {
        ApiSourceErrorVisible = string.IsNullOrEmpty(value);
    }

    partial void OnApiBaseUrlChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            ApiBaseUrlErrorVisible = true;
            return;
        }

        string pattern = @"(?!-)[A-Za-z0-9-]{1,63}(?<!-)";
        if (!Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase))
        {
            ApiBaseUrlErrorVisible = true;
        }
        else
        {
            ApiBaseUrlErrorVisible = false;
        }
    }

    partial void OnApiNameChanged(string? value)
    {
        ApiNameErrorVisible = string.IsNullOrEmpty(value);
    }
}