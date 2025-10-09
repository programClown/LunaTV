using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.Services;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSearchViewModel : ViewModelBase
{
    public const string LocalHost = "LocalHost";
    private readonly List<SearchResult> _allSearchResults = new();

    private readonly MovieTvService _apiService;

    private readonly LoadingWaitViewModel _loadingWaitViewModel = new();
    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private string? _inputMovieTvName;
    [ObservableProperty] private bool _isAdultMode;
    [ObservableProperty] private bool _isAdultVisible = true;
    [ObservableProperty] private string? _searchCountText = "0个结果";
    [ObservableProperty] private int _totalVideos;

    public TVShowSearchViewModel()
    {
        _apiService = App.Services.GetRequiredService<MovieTvService>();
        HistoryMovies = new ObservableCollection<string>();
        SearchResults = new ObservableCollection<SearchResult>();
    }

    public ObservableCollection<string> HistoryMovies { get; set; }
    public ObservableCollection<SearchResult> SearchResults { get; set; }
    public int PageSize { get; } = 12;

    public async Task Search(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        if (IsAdultMode) IsAdultMode = AppConifg.SelectAdultApis.Count > 0;

        if (!IsAdultMode && AppConifg.SelectApis.Count == 0)
            App.Notification?.Show(
                new Notification("没有选择任何源", $"查找\"{name}\"资源失败！", NotificationType.Warning),
                NotificationType.Warning,
                showClose: true);

        App.Notification?.Show(
            new Notification("查找", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);

        _ = Loading();
        SearchResults.Clear();
        _allSearchResults.Clear();
        CurrentPage = 1;

        if (IsAdultMode)
            foreach (var api in AppConifg.SelectAdultApis)
            {
                var ones = await _apiService.Search(api, name, true);
                _allSearchResults.AddRange(ones);
                ones.Take(PageSize - SearchResults.Count).ToList().ForEach(x => SearchResults.Add(x));

                if (_allSearchResults.Count >= AppConifg.SearchMaxVideos) break;
            }
        else
            foreach (var api in AppConifg.SelectApis)
            {
                var ones = await _apiService.Search(api, name);
                _allSearchResults.AddRange(ones);
                ones.Take(PageSize - SearchResults.Count).ToList().ForEach(x => SearchResults.Add(x));

                if (_allSearchResults.Count >= AppConifg.SearchMaxVideos) break;
            }

        SearchCountText = $"{_allSearchResults.Count}个结果";
        TotalVideos = _allSearchResults.Count;
        // SearchResults.AddRange(_allSearchResults.GetRange(0, int.Min(TotalVideos, PageSize)));
        _loadingWaitViewModel.Close();
    }

    public async Task ShowDetail(object? item)
    {
        if (item is not SearchResult searchResult) return;

        App.Notification?.Show(
            new Notification("找剧中", searchResult.Name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);
        _ = Loading();
        var videos = await _apiService.SearchDetail(searchResult.Source, searchResult.Id, IsAdultMode);

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
            StyleClass = ""
        };

        var vm = new TVShowDetailViewModel
        {
            VideoName = searchResult.Name,
            SourceName = searchResult.Source,
            VideoDetail = videos ?? new DetailResult(),
            IsVideoBorderVisible = videos?.Type is not null,
            EpisodesCountText = $"共{videos?.Episodes?.Count ?? 0}集"
        };
        vm.RefreshUi();

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
    private void LoadPage(int page)
    {
        CurrentPage = page;
        SearchResults.Clear();
        _allSearchResults.GetRange((page - 1) * PageSize,
            int.Min(PageSize, TotalVideos - (page - 1) * PageSize)).ForEach(x => SearchResults.Add(x));
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
            StyleClass = ""
        };

        _loadingWaitViewModel.TimerStart();

        await Dialog.ShowModal<LoadingWaitView, LoadingWaitViewModel>(_loadingWaitViewModel, options: options);
    }
}