using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.Base.Constants;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSettingViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ApiSourceItem> _commonApis;
    [ObservableProperty] private ObservableCollection<ApiSourceItem> _adultApis;
    [ObservableProperty] private ObservableCollection<ApiNetItem> _apiNets;

    public TVShowSettingViewModel()
    {
        CommonApis = new ObservableCollection<ApiSourceItem>();
        AdultApis = new ObservableCollection<ApiSourceItem>();
        ApiNets = new ObservableCollection<ApiNetItem>();
        int index = 0;
        foreach (var api in ApiSourceInfo.ApiSitesConfig)
        {
            if (api.Value.IsAdult)
            {
                AdultApis.Add(new ApiSourceItem
                {
                    Source = api.Key,
                    Name = api.Value.Name,
                    Enable = false,
                    IsCustom = api.Value.IsCustomApi,
                });
            }
            else
            {
                CommonApis.Add(new ApiSourceItem
                {
                    Source = api.Key,
                    Name = api.Value.Name,
                    Enable = false,
                    IsCustom = api.Value.IsCustomApi,
                });
            }

            ApiNets.Add(new ApiNetItem
            {
                Id = index++,
                Name = api.Value.Name,
                Url = api.Value.ApiBaseUrl,
                IsAdult = api.Value.IsAdult,
            });
        }
    }
}

public class ApiSourceItem
{
    public string? Source { get; set; }
    public string? Name { get; set; }
    public bool Enable { get; set; }
    public bool IsCustom { get; set; }
}

public class ApiNetItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public bool IsAdult { get; set; }
}