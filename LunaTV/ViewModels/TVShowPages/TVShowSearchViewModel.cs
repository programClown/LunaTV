using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Models;
using LunaTV.Services;
using LunaTV.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Nodify.Avalonia.Shared;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSearchViewModel : ViewModelBase
{
    public ObservableCollection<string> HistoryMovies { get; set; }
    public List<string> SelectApis { get; set; } = ["dyttzy", "tyyszy"];
    public ObservableCollection<SearchResult> SearchResults { get; set; }

    [ObservableProperty] private string? _inputMovieTvName;
    [ObservableProperty] private string? _searchCountText = "0个结果";
    private readonly MovieTvService _apiService;

    public TVShowSearchViewModel()
    {
        _apiService = App.Services.GetRequiredService<MovieTvService>();
        HistoryMovies = new ObservableCollection<string>();
        SearchResults = new ObservableCollection<SearchResult>();
    }

    public async Task Search(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        App.Notification?.Show(
            new Notification("查找", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);

        SearchResults.Clear();

        foreach (var api in SelectApis)
        {
            var ones = await _apiService.Search(api, name);
            SearchResults.AddRange(ones);
        }

        SearchCountText = $"{SearchResults.Count}个结果";
    }

    /// <summary>
    /// 显示详情
    /// </summary>
    /// <param name="vodId">视频id</param>
    /// <param name="vodName">视频名称</param>
    /// <param name="apiKey" ><see cref="ApiSourceInfo.ApiSitesConfig"/>的key</param>
    /// <param name="apiUrlAttr"><see cref="ApiSourceInfo.ApiSitesConfig"/>的ApiBaseUrl</param>
    private async void ShowDetail(string vodId, string vodName, string apiKey, string apiUrlAttr)
    {
        // var site = ApiSourceInfo.ApiSitesConfig[apiKey];
        // if (!string.IsNullOrEmpty(site.DetailPath)) //有detial
        // {
        //     var detail = _apiFactory
        //         .CreateRefitClient<IMovieTvApi>(new Uri(site.DetailBaseUrl));
        //     try
        //     {
        //         var html = await detail.GetSpecialSourceVideoDetail(vodId);
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //         throw;
        //     }
        // }
    }

    [RelayCommand]
    private void DeleteHistoty(string name)
    {
        HistoryMovies.Remove(name);
    }

    [RelayCommand]
    private void ClearAllHistories()
    {
        HistoryMovies.Clear();
    }

    [RelayCommand]
    private void Play(string name)
    {
        App.Notification.Show(
            new Notification("播放", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);
    }
}