using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Models;
using LunaTV.Services;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Nodify.Avalonia.Shared;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSearchViewModel : ViewModelBase
{
    public const string LocalHost = "LocalHost";
    public ObservableCollection<string> HistoryMovies { get; set; }
    public List<string> SelectApis { get; set; } = ["dyttzy", "tyyszy"];
    public ObservableCollection<SearchResult> SearchResults { get; set; }

    [ObservableProperty] private string? _inputMovieTvName;
    [ObservableProperty] private string? _searchCountText = "0个结果";
    private readonly MovieTvService _apiService;

    private readonly LoadingWaitViewModel _loadingWaitViewModel = new();

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

        _ = Loading();
        SearchResults.Clear();

        foreach (var api in SelectApis)
        {
            var ones = await _apiService.Search(api, name);
            SearchResults.AddRange(ones);
        }

        SearchCountText = $"{SearchResults.Count}个结果";
        _loadingWaitViewModel.Close();
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
        _ = Loading();
        var videos = await _apiService.SearchDetail(searchResult.Source, searchResult.Id);
        if (videos is not null)
        {
            if (videos.Episodes is { Count: > 0 })
            {
                // 显示播放列表
            }
        }

        _loadingWaitViewModel.Close();
        var options = new DialogOptions
        {
            Title = "",
            Mode = DialogMode.None,
            Button = DialogButton.None,
            ShowInTaskBar = false,
            IsCloseButtonVisible = true,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanDragMove = true,
            CanResize = false,
            StyleClass = "",
        };

        var vm = new TVShowDetailViewModel
        {
            VideoName = searchResult.Name,
            SourceName = searchResult.Source,
            VideoDetail = videos ?? new DetailResult(),
            IsVideoBorderVisible = videos?.Type is not null,
            EpisodesCountText = $"共{videos?.Episodes?.Count ?? 0}集",
        };

        await Dialog.ShowModal<TVShowDetailView, TVShowDetailViewModel>(vm, options: options);
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

    public async Task Loading()
    {
        var options = new DialogOptions
        {
            Title = "",
            Mode = DialogMode.None,
            Button = DialogButton.None,
            ShowInTaskBar = false,
            IsCloseButtonVisible = true,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanDragMove = true,
            CanResize = false,
            StyleClass = "",
        };

        _loadingWaitViewModel.TimerStart();

        await Dialog.ShowModal<LoadingWaitView, LoadingWaitViewModel>(_loadingWaitViewModel, options: options);
    }
}