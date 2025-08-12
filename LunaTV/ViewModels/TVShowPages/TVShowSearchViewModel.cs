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

    public async Task ShowDetail(object? item)
    {
        if (item is not SearchResult searchResult)
        {
            return;
        }

        App.Notification?.Show(
            new Notification("找剧中", searchResult.Name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);

        var videos = await _apiService.SearchDetail(searchResult.Source, searchResult.Id);
        if (videos is not null)
        {
            if (videos.Episodes is { Count: > 0 })
            {
                // 显示播放列表
            }
        }
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
    }
}