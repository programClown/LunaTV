using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
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
    private readonly HashSet<string> _searchResultKeys = new();
    private readonly ConcurrentDictionary<string, DetailResult> _searchDetails = new();
    private readonly ConcurrentDictionary<string, byte> _checkedSearchDetails = new();
    private readonly ConcurrentDictionary<string, byte> _searchDetailChecksInProgress = new();
    private readonly ConcurrentDictionary<string, byte> _failedSearchSources = new();
    private readonly ConcurrentDictionary<string, byte> _skippedDeadSources = new();
    private int _autoPageSize = 16;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private List<string> _searchSources = [];
    private string? _currentSearchName;
    private bool _currentSearchIsAdult;
    private bool _isShowingDetail;
    private bool _stopRequestedByUser;
    private int _nextSourceIndex;
    private int _nextPage = 1;
    private readonly List<Task> _pendingDetailTasks = new();


    private readonly MovieTvService _apiService;
    private readonly SugarRepository<SearchHistory> _searchHistoryTable;
    private readonly SugarRepository<MediaDownload> _mediaDownloadTable;
    private readonly SugarRepository<ViewHistory> _viewHistoryTable;

    private readonly LoadingWaitViewModel _loadingWaitViewModel = new();
    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private string? _inputMovieTvName;
    [ObservableProperty] private bool _isAdultMode;
    [ObservableProperty] private bool _isAdultVisible = true;
    [ObservableProperty] private string? _searchCountText = "共 0 个结果";
    [ObservableProperty] private int _totalVideos;
    [ObservableProperty] private int _pageSize = 16;
    [ObservableProperty] private bool _isAutoPageSize = true;
    [ObservableProperty] private int _manualPageSize = 16;
    [ObservableProperty] private bool _isManualPageSizeEnabled;
    [ObservableProperty] private double _searchResultCardWidth = 300;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isSearchPaused;
    [ObservableProperty] private bool _isSearchCompleted = true;
    [ObservableProperty] private bool _canSearch = true;

    public TVShowSearchViewModel()
    {
        _apiService = App.Services.GetRequiredService<MovieTvService>();
        _searchHistoryTable = App.Services.GetRequiredService<SugarRepository<SearchHistory>>();
        _mediaDownloadTable = App.Services.GetRequiredService<SugarRepository<MediaDownload>>();
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
        HistoryMovies = new ObservableCollection<string>();
        SearchResults = new ObservableCollection<SearchResult>();
        Dispatcher.UIThread.InvokeAsync(async () => await LoadSearchHistoriesAsync());
    }

    public ObservableCollection<string> HistoryMovies { get; set; }
    public ObservableCollection<SearchResult> SearchResults { get; set; }

    private async Task LoadSearchHistoriesAsync()
    {
        var histories = await _searchHistoryTable.Context.Queryable<SearchHistory>()
            .Where(history => history.MovieName != null && history.MovieName != "")
            .OrderByDescending(history => history.CreateTime)
            .Take(20)
            .ToListAsync();

        HistoryMovies.Clear();
        foreach (var history in histories)
        {
            if (!string.IsNullOrWhiteSpace(history.MovieName))
                HistoryMovies.Add(history.MovieName);
        }
    }

    private async Task SaveSearchHistoryAsync(string name)
    {
        await _searchHistoryTable.DeleteAsync(item => item.MovieName == name);
        await _searchHistoryTable.InsertAsync(new SearchHistory
        {
            MovieName = name,
            CreateTime = DateTime.Now
        });

        var oldHistories = await _searchHistoryTable.Context.Queryable<SearchHistory>()
            .OrderByDescending(item => item.CreateTime)
            .Skip(100)
            .ToListAsync();
        foreach (var oldHistory in oldHistories)
        {
            await _searchHistoryTable.DeleteByIdAsync(oldHistory.Id);
        }
    }

    partial void OnIsAutoPageSizeChanged(bool value)
    {
        IsManualPageSizeEnabled = !value;
        if (!value)
        {
            ManualPageSize = PageSize;
            return;
        }

        ApplyPageSize(_autoPageSize);
    }

    partial void OnManualPageSizeChanged(int value)
    {
        if (IsAutoPageSize) return;

        var normalizedPageSize = NormalizePageSize(value);
        if (value != normalizedPageSize)
        {
            ManualPageSize = normalizedPageSize;
            return;
        }

        ApplyPageSize(value);
    }

    [RelayCommand]
    public async Task Search(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        if (IsSearching)
        {
            StopCurrentSearch();
            var timeout = TimeSpan.FromSeconds(5);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (IsSearching && sw.Elapsed < timeout)
            {
                await Task.Delay(100);
            }
        }

        if (IsAdultMode) IsAdultMode = AppConifg.SelectAdultApis.Count > 0;
        if (!IsAdultMode && AppConifg.SelectApis.Count == 0 && AppConifg.SelectAdultApis.Count > 0) IsAdultMode = true;
        _searchSources = (IsAdultMode ? AppConifg.SelectAdultApis : AppConifg.SelectApis).ToList();
        if (_searchSources.Count == 0)
        {
            App.Notification?.Show(
                new Notification("没有选择任何源", $"查找\"{name}\"资源失败！", NotificationType.Warning),
                NotificationType.Warning,
                showClose: true);
            return;
        }

        App.Notification?.Show(
            new Notification("查找", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);

        _currentSearchName = name;
        _currentSearchIsAdult = IsAdultMode;
        _nextSourceIndex = 0;
        _nextPage = 1;
        _failedSearchSources.Clear();
        _skippedDeadSources.Clear();
        _searchResultKeys.Clear();
        _searchDetails.Clear();
        _checkedSearchDetails.Clear();
        _searchDetailChecksInProgress.Clear();
        SearchResults.Clear();
        _allSearchResults.Clear();
        CurrentPage = 1;
        TotalVideos = 0;
        IsSearchPaused = false;
        IsSearchCompleted = false;
        SearchCountText = "搜索中，共 0 个结果";

        if (!HistoryMovies.Contains(name))
        {
            HistoryMovies.Insert(0, name);
            // Keep only last 20 entries
            while (HistoryMovies.Count > 20)
                HistoryMovies.RemoveAt(HistoryMovies.Count - 1);
        }
        else
        {
            HistoryMovies.Move(HistoryMovies.IndexOf(name), 0);
        }

        await SaveSearchHistoryAsync(name);

        _ = RunSearchAsync();
    }

    public void StopCurrentSearch(bool pause = false)
    {
        _stopRequestedByUser = pause;
        _searchCancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private void StopSearch()
    {
        StopCurrentSearch(true);
    }

    [RelayCommand]
    private async Task ContinueSearch()
    {
        if (IsSearching || !IsSearchPaused || string.IsNullOrWhiteSpace(_currentSearchName)) return;
        _failedSearchSources.Clear();
        _skippedDeadSources.Clear();
        await RunSearchAsync();
    }

    private async Task RunSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentSearchName)) return;

        IsSearching = true;
        CanSearch = false;
        IsSearchPaused = false;
        SearchCountText = $"搜索中，共 {_allSearchResults.Count} 个结果";
        _searchCancellationTokenSource = new CancellationTokenSource();
        var cts = _searchCancellationTokenSource;
        var token = cts.Token;

        lock (_pendingDetailTasks) { _pendingDetailTasks.Clear(); }

        try
        {
            var sourceTasks = _searchSources.Select(source => SearchSourceAsync(source, token)).ToArray();
            await Task.WhenAll(sourceTasks);
            await SearchLocalAsync(_currentSearchName, token);

            if (ReferenceEquals(_searchCancellationTokenSource, cts))
            {
                IsSearchCompleted = true;
                SearchCountText = BuildSearchCountText(false);
            }
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_searchCancellationTokenSource, cts))
            {
                IsSearchPaused = _stopRequestedByUser;
                IsSearchCompleted = false;
                var stoppedText = $"已停止，共 {_allSearchResults.Count} 个结果";
                var failedCount = _failedSearchSources.Count;
                var skippedCount = _skippedDeadSources.Count;
                if (failedCount > 0 && skippedCount > 0)
                    SearchCountText = $"{stoppedText}，{failedCount} 个源失败，{skippedCount} 个源暂时不可用";
                else if (failedCount > 0)
                    SearchCountText = $"{stoppedText}，{failedCount} 个源失败";
                else if (skippedCount > 0)
                    SearchCountText = $"{stoppedText}，{skippedCount} 个源暂时不可用";
                else
                    SearchCountText = stoppedText;
            }
        }
        finally
        {
            cts.Dispose();
            if (ReferenceEquals(_searchCancellationTokenSource, cts))
            {
                _stopRequestedByUser = false;
                _searchCancellationTokenSource = null;
                IsSearching = false;
                CanSearch = true;

                lock (_pendingDetailTasks)
                {
                    _pendingDetailTasks.Clear();
                }
            }
        }
    }

    private async Task SearchLocalAsync(string searchName, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var downloads = await _mediaDownloadTable.Context.Queryable<MediaDownload>()
            .Where(download => download.IsDownloaded)
            .ToListAsync();
        var downloadResults = downloads
            .Select(download => new
            {
                Download = download,
                FilePath = DownloadFileResolver.ResolveExistingFile(download.OutputFilePath, download.LocalPath, download.Name, download.Episode)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.FilePath))
            .Select(item => new
            {
                item.Download,
                FilePath = item.FilePath!
            })
            .Where(item => MatchesLocalSearch(searchName, item.Download.Name, item.Download.Episode, item.Download.Source, item.FilePath, item.Download.Url))
            .Select(item => new SearchResult
            {
                Id = $"download:{item.Download.Id}",
                Source = LocalHost,
                SourceName = "本地下载",
                Name = string.IsNullOrWhiteSpace(item.Download.Name) ? Path.GetFileName(item.FilePath) : item.Download.Name!,
                Tag = string.IsNullOrWhiteSpace(item.Download.Episode) ? "已下载" : item.Download.Episode!,
                ReMark = item.FilePath,
                Descriptor = item.Download.Source ?? string.Empty
            })
            .ToList();

        var localHistories = await _viewHistoryTable.Context.Queryable<ViewHistory>()
            .Where(history => history.IsLocal)
            .ToListAsync();
        var localResults = localHistories
            .Where(history => MatchesLocalSearch(searchName, history.Name, history.Episode, history.Source, history.Url, history.VodId))
            .Select(history => new SearchResult
            {
                Id = $"local:{history.Id}",
                Source = LocalHost,
                SourceName = history.Source == "下载" ? "本地下载" : "本地视频",
                Name = string.IsNullOrWhiteSpace(history.Name) ? Path.GetFileName(history.Url ?? string.Empty) : history.Name!,
                Tag = string.IsNullOrWhiteSpace(history.Episode) ? "本地" : history.Episode!,
                ReMark = history.Url ?? string.Empty,
                Cover = history.Cover ?? string.Empty,
                Descriptor = history.Source ?? "本地"
            })
            .ToList();

        await AppendSearchResultsAsync(downloadResults.Concat(localResults), token);
    }

    private static bool MatchesLocalSearch(string searchName, params string?[] values)
    {
        return values.Any(value => value?.Contains(searchName, StringComparison.OrdinalIgnoreCase) == true);
    }

    private async Task SearchSourceAsync(string source, CancellationToken token)
    {
        var page = 1;
        try
        {
            while (_allSearchResults.Count < AppConifg.SearchMaxVideos)
            {
                token.ThrowIfCancellationRequested();

                if (MovieTvService.IsHostDead(source))
                {
                    _skippedDeadSources.TryAdd(source, 0);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        SearchCountText = BuildSearchCountText(true));
                    return;
                }

                var (results, pageCount) = await _apiService.SearchPage(source, _currentSearchName!, page, _currentSearchIsAdult, token);

                await AppendSearchResultsAsync(results, token);
                if (results.Count == 0 && pageCount == 0)
                {
                    await MarkSearchSourceFailedAsync(source);
                    return;
                }

                if (page >= pageCount || page >= AppConifg.SearchMaxPages || pageCount <= 0) return;
                page++;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await MarkSearchSourceFailedAsync(source);
        }
    }

    private async Task MarkSearchSourceFailedAsync(string source)
    {
        _failedSearchSources.TryAdd(source, 0);
        await Dispatcher.UIThread.InvokeAsync(() =>
            SearchCountText = $"{source} 搜索失败，继续搜索中，共 {_allSearchResults.Count} 个结果");
    }

    private async Task AppendSearchResultsAsync(IEnumerable<SearchResult> results, CancellationToken cancellationToken)
    {
        await Dispatcher.UIThread.InvokeAsync(() => AppendSearchResults(results, cancellationToken));
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private void AppendSearchResults(IEnumerable<SearchResult> results, CancellationToken cancellationToken)
    {
        var startVisible = (CurrentPage - 1) * PageSize;
        var endVisible = startVisible + PageSize;

        // Phase 1: Add all results immediately without fetching details
        var newResults = new List<SearchResult>();
        foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(result.Id)) continue;

            var key = BuildSearchResultKey(result);
            if (_searchResultKeys.Contains(key)) continue;

            _searchResultKeys.Add(key);
            newResults.Add(result);
            var index = _allSearchResults.Count;
            _allSearchResults.Add(result);
            if (index >= startVisible && index < endVisible) SearchResults.Add(result);
            if (_allSearchResults.Count >= AppConifg.SearchMaxVideos) break;
        }

        TotalVideos = _allSearchResults.Count;
        SearchCountText = BuildSearchCountText(IsSearching);
        NormalizeManualPageSize();

        if (newResults.Count == 0) return;

        var visiblePrefetchResults = newResults
            .Where(result =>
            {
                var index = _allSearchResults.IndexOf(result);
                return index >= startVisible && index < endVisible;
            })
            .ToArray();
        QueueSearchDetailChecks(visiblePrefetchResults, cancellationToken);
    }

    private void QueueSearchDetailChecks(IEnumerable<SearchResult> results, CancellationToken cancellationToken)
    {
        var uncheckedResults = results
            .Where(result => result.Source != LocalHost)
            .Where(result =>
            {
                var key = BuildSearchResultKey(result);
                if (_checkedSearchDetails.ContainsKey(key) || _searchDetailChecksInProgress.ContainsKey(key)) return false;
                _searchDetailChecksInProgress.TryAdd(key, 0);
                return true;
            })
            .ToArray();
        if (uncheckedResults.Length == 0) return;

        var detailTasks = uncheckedResults.Select(result => CheckSearchDetailAsync(result, cancellationToken)).ToArray();
        lock (_pendingDetailTasks)
        {
            _pendingDetailTasks.AddRange(detailTasks);
        }
    }

    private async Task CheckSearchDetailAsync(SearchResult result, CancellationToken cancellationToken)
    {
        var key = BuildSearchResultKey(result);
        try
        {
            var detail = await _apiService.SearchDetail(result.Source, result.Id, _currentSearchIsAdult, cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _checkedSearchDetails.TryAdd(key, 0);
                if (detail?.Episodes is { Count: > 0 })
                {
                    _searchDetails[key] = detail;
                }
                else
                {
                    RemoveSearchResult(result);
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => _searchDetailChecksInProgress.TryRemove(key, out _));
        }
    }

    private string BuildSearchCountText(bool isSearching)
    {
        var text = isSearching
            ? $"搜索中，共 {_allSearchResults.Count} 个结果"
            : $"共 {_allSearchResults.Count} 个结果";

        var failedCount = _failedSearchSources.Count;
        var skippedCount = _skippedDeadSources.Count;

        if (failedCount > 0 && skippedCount > 0)
            return $"{text}，{failedCount} 个源失败，{skippedCount} 个源暂时不可用";
        if (failedCount > 0)
            return $"{text}，{failedCount} 个源失败";
        if (skippedCount > 0)
            return $"{text}，{skippedCount} 个源暂时不可用";
        return text;
    }

    private static string BuildSearchResultKey(SearchResult result)
    {
        return $"{result.Source}:id:{result.Id}";
    }

    private void RemoveSearchResult(SearchResult searchResult)
    {
        _allSearchResults.Remove(searchResult);
        SearchResults.Remove(searchResult);
        var key = BuildSearchResultKey(searchResult);
        _searchResultKeys.Remove(key);
        _searchDetails.TryRemove(key, out _);
        _checkedSearchDetails.TryRemove(key, out _);
        _searchDetailChecksInProgress.TryRemove(key, out _);
        TotalVideos = _allSearchResults.Count;
        SearchCountText = BuildSearchCountText(IsSearching);
        NormalizeManualPageSize();
        if (!IsSearching) RefreshCurrentPage();
    }

    [RelayCommand]
    public async Task ShowDetail(object? item)
    {
        if (_isShowingDetail) return;
        if (item is not SearchResult searchResult) return;

        StopCurrentSearch();
        _isShowingDetail = true;
        try
        {
            await ShowDetailCore(searchResult);
        }
        finally
        {
            _isShowingDetail = false;
        }
    }

    private async Task ShowDetailCore(SearchResult searchResult)
    {
        if (searchResult.Source == LocalHost)
        {
            await ShowLocalDetailCore(searchResult);
            return;
        }

        App.Notification?.Show(
            new Notification("找剧中", searchResult.Name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);
        var key = BuildSearchResultKey(searchResult);
        var isCached = _searchDetails.TryGetValue(key, out var cachedDetail)
                       && cachedDetail?.Episodes is { Count: > 0 };

        if (!isCached)
            _ = Loading().ContinueWith(t => System.Diagnostics.Trace.WriteLine($"[SearchDetail] Loading异常: {t.Exception?.InnerException?.Message}"), TaskContinuationOptions.OnlyOnFaulted);

        try
        {
            using var detailCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var videos = isCached
                ? cachedDetail
                : await _apiService.SearchDetail(searchResult.Source, searchResult.Id, IsAdultMode, detailCts.Token);

            if (videos?.Episodes is not { Count: > 0 })
            {
                RemoveSearchResult(searchResult);
                App.Notification?.Show(
                    new Notification("没有可播放视频", $"{searchResult.Name} 没有可播放剧集，已从结果中移除。", NotificationType.Information),
                    NotificationType.Information,
                    showClose: true);
                return;
            }

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
                Cover = searchResult.Cover,
                VideoDetail = videos ?? new DetailResult(),
                IsVideoBorderVisible = videos?.Type is not null,
                EpisodesCountText = $"共{videos?.Episodes?.Count ?? 0}集"
            };
            await vm.RefreshUiAsync();

#if ANDROID
            // On Android, show detail as a page instead of a modal dialog
            var mainViewModel = App.Services.GetRequiredService<MainViewModel>();
            var prevPage = mainViewModel.PageContent;
            var detailView = new TVShowDetailView { DataContext = vm };
            mainViewModel.PageContent = detailView;
            // The detail view has its own back/close mechanism via IDialogContext.Close()
            vm.RequestClose += (_, _) => mainViewModel.PageContent = prevPage;
#else
            await Dialog.ShowModal<TVShowDetailView, TVShowDetailViewModel>(vm, options: options);
#endif
        }
        catch (OperationCanceledException)
        {
            App.Notification?.Show(
                new Notification("详情加载超时", $"{searchResult.Name} 详情加载超时，请稍后重试。", NotificationType.Warning),
                NotificationType.Warning,
                showClose: true);
        }
        finally
        {
            _loadingWaitViewModel.Close();
#if ANDROID
            IsSearching = false;
#endif
        }
    }

    private async Task ShowLocalDetailCore(SearchResult searchResult)
    {
        var detail = await BuildLocalDetailAsync(searchResult);
        if (detail?.Episodes is not { Count: > 0 })
        {
            App.Notification?.Show(
                new Notification("没有可播放视频", $"{searchResult.Name} 没有可播放的本地文件。", NotificationType.Information),
                NotificationType.Information,
                showClose: true);
            return;
        }

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
            VideoName = detail.Title ?? searchResult.Name,
            SourceName = LocalHost,
            Cover = detail.Cover,
            VideoDetail = detail,
            IsVideoBorderVisible = false,
            EpisodesCountText = $"共{detail.Episodes.Count}集"
        };
        await vm.RefreshUiAsync();
        foreach (var episode in vm.Episodes)
        {
            episode.IsDownloaded = true;
            episode.OutputFilePath = episode.Url;
        }

        await Dialog.ShowModal<TVShowDetailView, TVShowDetailViewModel>(vm, options: options);
    }

    private async Task<DetailResult?> BuildLocalDetailAsync(SearchResult searchResult)
    {
        if (searchResult.Id.StartsWith("download:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(searchResult.Id["download:".Length..], out var downloadId))
        {
            var download = await _mediaDownloadTable.GetByIdAsync(downloadId);
            if (download is null) return null;
            var filePath = DownloadFileResolver.ResolveExistingFile(download.OutputFilePath, download.LocalPath, download.Name, download.Episode);
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            if (download.OutputFilePath != filePath)
            {
                download.OutputFilePath = filePath;
                await _mediaDownloadTable.UpdateAsync(download);
            }

            return new DetailResult
            {
                VodId = filePath,
                Title = string.IsNullOrWhiteSpace(download.Name) ? Path.GetFileName(filePath) : download.Name,
                Source = LocalHost,
                SourceName = "本地下载",
                Cover = searchResult.Cover,
                Episodes =
                [
                    new EpisodeSubject
                    {
                        Name = string.IsNullOrWhiteSpace(download.Episode) ? Path.GetFileName(filePath) : download.Episode,
                        Url = filePath
                    }
                ]
            };
        }

        if (searchResult.Id.StartsWith("local:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(searchResult.Id["local:".Length..], out var historyId))
        {
            var history = await _viewHistoryTable.GetByIdAsync(historyId);
            if (history is null || string.IsNullOrWhiteSpace(history.Url)) return null;
            return new DetailResult
            {
                VodId = history.Url,
                Title = string.IsNullOrWhiteSpace(history.Name) ? Path.GetFileName(history.Url) : history.Name,
                Source = LocalHost,
                SourceName = history.Source ?? "本地视频",
                Cover = history.Cover,
                Episodes =
                [
                    new EpisodeSubject
                    {
                        Name = string.IsNullOrWhiteSpace(history.Episode) ? Path.GetFileName(history.Url) : history.Episode,
                        Url = history.Url
                    }
                ]
            };
        }

        return null;
    }

    [RelayCommand]
    private async Task DeleteHistoty(string name)
    {
        HistoryMovies.Remove(name);
        await _searchHistoryTable.DeleteAsync(history => history.MovieName == name);
    }

    [RelayCommand]
    private async Task ClearAllHistories()
    {
        HistoryMovies.Clear();
        await _searchHistoryTable.Context.Deleteable<SearchHistory>().Where(history => true).ExecuteCommandAsync();
    }

    [RelayCommand]
    private void LoadPage(int page)
    {
        CurrentPage = page;
        RefreshCurrentPage();
    }

    public void UpdatePageSize(double width, double height)
    {
        if (!IsAutoPageSize) return;
        if (width <= 0 || height <= 0) return;

        const double itemMinWidth = 300;
        const double itemHeight = 168;
        const double itemSpacing = 8;

        var columns = int.Max(1, (int)(width / (itemMinWidth + itemSpacing)));
        var rows = int.Max(1, (int)(height / itemHeight));
        SearchResultCardWidth = width < itemMinWidth
            ? itemMinWidth
            : (width - itemSpacing - itemSpacing * columns) / columns;
        _autoPageSize = columns * rows;
        ManualPageSize = _autoPageSize;
        ApplyPageSize(_autoPageSize);
    }

    private void ApplyPageSize(int value)
    {
        var newPageSize = int.Max(1, value);

        if (newPageSize == PageSize) return;

        PageSize = newPageSize;

        if (_allSearchResults.Count == 0) return;

        var maxPage = int.Max(1, (int)Math.Ceiling((double)_allSearchResults.Count / PageSize));
        if (CurrentPage > maxPage) CurrentPage = maxPage;

        RefreshCurrentPage();
    }

    private int NormalizePageSize(int value)
    {
        var maxPageSize = int.Max(1, _allSearchResults.Count);
        return int.Clamp(value, 1, maxPageSize);
    }

    private void NormalizeManualPageSize()
    {
        if (IsAutoPageSize) return;

        var normalizedPageSize = NormalizePageSize(ManualPageSize);
        if (ManualPageSize != normalizedPageSize) ManualPageSize = normalizedPageSize;
    }

    private void RefreshCurrentPage()
    {
        if (IsSearching) return;

        SearchResults.Clear();

        var start = (CurrentPage - 1) * PageSize;
        if (start >= _allSearchResults.Count) return;

        var pageResults = _allSearchResults
            .Skip(start)
            .Take(PageSize)
            .ToList();
        pageResults.ForEach(x => SearchResults.Add(x));
        QueueSearchDetailChecks(pageResults, _searchCancellationTokenSource?.Token ?? CancellationToken.None);
    }

    public async Task Loading()
    {
#if ANDROID
        IsSearching = true;
        return;
#else
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
#endif
    }
}