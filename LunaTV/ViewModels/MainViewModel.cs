using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.TVShowPages;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Dictionary<string, UserControl> _viewDictionary;

    private readonly IWebApi _webApi;

    [ObservableProperty] private bool _blockedLoading;
    [ObservableProperty] private UserControl? _pageContent;
    [ObservableProperty] private TVMenuItem? _selectedItem;

    public MainViewModel(IWebApi webApi)
    {
        _webApi = webApi;

        Items = new ObservableCollection<TVMenuItem>
        {
            new()
            {
                Name = "首页",
                Data = App.TopLevel?.TryFindResource("SemiIconHome", out var value1) == true ? (StreamGeometry)value1! : null
            },
            new()
            {
                Name = "搜索",
                Data = App.TopLevel?.TryFindResource("SemiIconSearch", out var value2) == true ? (StreamGeometry)value2! : null
            },
            // new()
            // {
            //     Name = "筛选",
            //     Data = App.TopLevel?.TryFindResource("SemiIconFilter", out var value3) == true ? (StreamGeometry)value3! : null,
            // },
            new()
            {
                Name = "历史",
                Data = App.TopLevel?.TryFindResource("SemiIconHistory", out var value4) == true ? (StreamGeometry)value4! : null
            },
            new()
            {
                Name = "下载",
                Data = App.TopLevel?.TryFindResource("SemiIconDownload", out var value5) == true ? (StreamGeometry)value5! : null
            },
            new()
            {
                Name = "配置",
                Data = App.TopLevel?.TryFindResource("SemiIconSetting", out var value6) == true ? (StreamGeometry)value6! : null
            }
        };


        //初始化配置
        try
        {
            AppConifg.SelectApis.Clear();
            var apiSourceTable = App.Services.GetRequiredService<SugarRepository<ApiSource>>();
            var apiSources = apiSourceTable.GetList();
            AppConifg.SelectApis.Clear();
            AppConifg.SelectAdultApis.Clear();
            AppConifg.SelectApis.AddRange(apiSources.Where(api => api.IsEnable && !api.IsAdult).Select(api => api.Source));
            AppConifg.SelectAdultApis.AddRange(apiSources.Where(api => api.IsEnable && api.IsAdult)
                .Select(api => api.Source));
            AppConifg.UpdateSites(apiSources);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] API config init failed: {ex.Message}");
        }

        try
        {
            var playerConfigTable = App.Services.GetRequiredService<SugarRepository<PlayerConfig>>();
            var playerConfig = playerConfigTable.GetList(config => config.Id > 0)
                .OrderByDescending(config => config.Id)
                .FirstOrDefault();
            if (playerConfig is null)
            {
                AppConifg.PlayerConfig = new PlayerConfig
                {
                    AdFilteringEnabled = true,
                    DoubanApiEnabled = false,
                    HomeAutoLoadDoubanEnabled = false,
                    ForceApiNeedSpecialSource = false,
                    Timeout = 15000,
                    FilterAds = true,
                    AutoPlayNext = false
                };
                playerConfigTable.Insert(AppConifg.PlayerConfig);
        }
        else
        {
            AppConifg.PlayerConfig = playerConfig;
        }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] PlayerConfig init failed: {ex.Message}");
            AppConifg.PlayerConfig ??= new PlayerConfig
            {
                AdFilteringEnabled = true, DoubanApiEnabled = false,
                HomeAutoLoadDoubanEnabled = false, ForceApiNeedSpecialSource = false,
                Timeout = 15000, FilterAds = true, AutoPlayNext = false
            };
        }

        _viewDictionary = new()
        {
            ["首页"] = new TVShowHomeView
            {
                DataContext = new TVShowHomeViewModel()
            },
            ["搜索"] = new TVShowSearchView
            {
                DataContext = new TVShowSearchViewModel()
            },
            // ["筛选"] = new TVShowFilterView
            // {
            //     DataContext = new TVShowFilterViewModel()
            // },
            ["历史"] = App.Services.GetRequiredService<TVShowHistoryView>(),
            ["下载"] = App.Services.GetRequiredService<TVDownloadView>(),
            ["配置"] = new TVShowSettingView
            {
                DataContext = new TVShowSettingViewModel()
            }
        };
        SelectedItem = Items[0];
    }

    public ObservableCollection<TVMenuItem> Items { get; set; }

    partial void OnSelectedItemChanged(TVMenuItem? value)
    {
        if (value == null) return;
        ToView(value.Name);
    }

    [RelayCommand]
    private void ToView(string content)
    {
        if (string.IsNullOrEmpty(content)) return;
        if (_viewDictionary.TryGetValue(content, out var control))
        {
            if (control.DataContext is TVShowHistoryViewModel historyViewModel)
                historyViewModel.RefreshHistoryItems();
            PageContent = control;
        }
        else
        {
            PageContent = content switch
            {
                "历史" => App.Services.GetRequiredService<TVShowHistoryView>(),
                "下载" => App.Services.GetRequiredService<TVDownloadView>(),
                "配置" => new TVShowSettingView
                {
                    DataContext = new TVShowSettingViewModel()
                },
                _ => null
            };
        }

        if (content == "历史")
        {
            App.Services.GetRequiredService<TVShowHistoryViewModel>().RefreshHistoryItems();
        }
    }

    public UserControl GetControl(string name)
    {
        return _viewDictionary[name];
    }

    public void RefreshHistory()
    {
        if (_viewDictionary.TryGetValue("历史", out var control)
            && control.DataContext is TVShowHistoryViewModel historyViewModel)
        {
            historyViewModel.RefreshHistoryItems();
        }
    }
}

public class TVMenuItem
{
    public string Name { get; set; }
    public StreamGeometry Data { get; set; }
}
