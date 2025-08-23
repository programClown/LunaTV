using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.Base.Api;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.TVShowPages;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Nodify.Avalonia.Shared;
using Ursa.Controls;

namespace LunaTV.ViewModels;

public partial class TVShowViewModel : PageViewModelBase
{
    private readonly IWebApi _webApi;
    public override string Title => "无限影视";

    [ObservableProperty] private bool _blockedLoading;

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("VideoIcon", out var value) ? (IconSource)value : null;

    private readonly Dictionary<string, UserControl> _viewDictionary = new()
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
        // ["历史"] = new TVShowHistoryView
        // {
        //     DataContext = new TVShowHistoryViewModel()
        // },
        // ["配置"] = new TVShowSettingView
        // {
        //     DataContext = new TVShowSettingViewModel()
        // }
    };

    public ObservableCollection<TVMenuItem> Items { get; set; }
    [ObservableProperty] private TVMenuItem? _selectedItem;
    [ObservableProperty] private UserControl? _pageContent;

    public TVShowViewModel(IWebApi webApi)
    {
        _webApi = webApi;

        Items = new ObservableCollection<TVMenuItem>()
        {
            new()
            {
                Name = "首页",
                Data = App.TopLevel.TryFindResource("SemiIconHome", out var value1) ? (StreamGeometry)value1 : null,
            },
            new()
            {
                Name = "搜索",
                Data = App.TopLevel.TryFindResource("SemiIconSearch", out var value2) ? (StreamGeometry)value2 : null,
            },
            // new()
            // {
            //     Name = "筛选",
            //     Data = App.TopLevel.TryFindResource("SemiIconFilter", out var value3) ? (StreamGeometry)value3 : null,
            // },
            new()
            {
                Name = "历史",
                Data = App.TopLevel.TryFindResource("SemiIconHistory", out var value4) ? (StreamGeometry)value4 : null,
            },
            new()
            {
                Name = "配置",
                Data = App.TopLevel.TryFindResource("SemiIconSetting", out var value5) ? (StreamGeometry)value5 : null,
            },
        };
        SelectedItem = Items[0];

        //初始化配置
        AppConifg.SelectApis.Clear();
        var apiSourceTable = App.Services.GetRequiredService<SugarRepository<ApiSource>>();
        var apiSources = apiSourceTable.GetList();
        AppConifg.SelectApis.Clear();
        AppConifg.SelectAdultApis.Clear();
        AppConifg.SelectApis.AddRange(apiSources.Where(api => api.IsEnable && !api.IsAdult).Select(api => api.Source));
        AppConifg.SelectAdultApis.AddRange(apiSources.Where(api => api.IsEnable && api.IsAdult)
            .Select(api => api.Source));
    }

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
            PageContent = control;
        }
        else
        {
            PageContent = content switch
            {
                "历史" => new TVShowHistoryView
                {
                    DataContext = new TVShowHistoryViewModel()
                },
                "配置" => new TVShowSettingView
                {
                    DataContext = new TVShowSettingViewModel()
                },
                _ => null
            };
        }
    }

    public UserControl GetControl(string name)
    {
        return _viewDictionary[name];
    }
}

public class TVMenuItem
{
    public string Name { get; set; }
    public StreamGeometry Data { get; set; }
}