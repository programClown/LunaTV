using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHomeViewModel : ViewModelBase
{
    private int _pageSize = 16;
    private int _autoPageSize = 16;
    private const int DoubanPageLimit = 16;
    private static readonly JsonSerializerOptions DoubanJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly SugarRepository<ApiSource> _apiSourceTable;

    private readonly List<string> _defaultMovieTags =
        ["热门", "最新", "经典", "豆瓣高分", "冷门佳片", "华语", "欧美", "韩国", "日本", "动作", "喜剧", "日综", "爱情", "科幻", "悬疑", "恐怖", "治愈"];

    private readonly List<string> _defaultTvTags =
        ["热门", "美剧", "英剧", "韩剧", "日剧", "国产剧", "港剧", "日本动画", "综艺", "纪录片"];

    private readonly bool _initialized;

    private readonly LoadingWaitViewModel _loadingWaitViewModel = new();
    private readonly SemaphoreSlim _movieCardImageLoadSemaphore = new(4);
    private bool _isRefreshingMovieCards;
    private bool _refreshMovieCardsAgain;
    private CancellationTokenSource? _pageSizeRefreshDebounceTokenSource;
    private CancellationTokenSource? _naviSearchDebounceCts;
#if !ANDROID
    private DoubanVerifyWindow? _doubanVerifyWindow;
#endif

    [ObservableProperty] private ObservableCollection<string> _doubanTags;
    private bool _isTagChanged2Refresh; //标签改变的时候要不要更新

    [ObservableProperty] private ObservableCollection<MovieCardItem> _movieCardItems;
    [ObservableProperty] private bool _movieChecked = true;
    [ObservableProperty] private bool _isLoading;
    private int _pageStart;
    [ObservableProperty] private bool _isAutoPageSize = true;
    [ObservableProperty] private int _manualPageSize = 16;
    [ObservableProperty] private bool _isManualPageSizeEnabled;
    [ObservableProperty] private double _movieCardWidth = 150;
    [ObservableProperty] private string? _searchInputText;
    [ObservableProperty] private string? _selectedTagItem;
    private string _switchMovieOrTv = "movie";

    public TVShowHomeViewModel()
    {
        DoubanTags = new ObservableCollection<string>();
        MovieCardItems = new ObservableCollection<MovieCardItem>();
        _ = SwitchMovieOrTv("电影");

        _initialized = true;

        try
        {
            _apiSourceTable = App.Services.GetRequiredService<SugarRepository<ApiSource>>();
            var apiSources = _apiSourceTable.GetList();
            AppConifg.UpdateSites(apiSources);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] TVShowHomeViewModel DB init failed: {ex.Message}");
        }
    }

    partial void OnIsAutoPageSizeChanged(bool value)
    {
        IsManualPageSizeEnabled = !value;
        if (!value)
        {
            ManualPageSize = _pageSize;
            return;
        }

        ApplyPageSize(_autoPageSize);
    }

    partial void OnManualPageSizeChanged(int value)
    {
        if (IsAutoPageSize) return;

        var newPageSize = int.Max(1, value);
        if (value != newPageSize)
        {
            ManualPageSize = newPageSize;
            return;
        }

        ApplyPageSize(value);
    }

    public void UpdatePageSize(double width, double height)
    {
        if (!IsAutoPageSize) return;
        if (width <= 0 || height <= 0) return;

        const double itemMinWidth = 150;
        const double itemHeight = 285;
        const double itemSpacing = 8;

        var columns = int.Max(1, (int)(width / (itemMinWidth + itemSpacing)));
        var rows = int.Max(1, (int)(height / itemHeight));
        MovieCardWidth = width < itemMinWidth
            ? itemMinWidth
            : (width - itemSpacing - itemSpacing * columns) / columns;
        _autoPageSize = columns * rows;
        ManualPageSize = _autoPageSize;
        ApplyPageSize(_autoPageSize);
    }

    private void ApplyPageSize(int value)
    {
        var newPageSize = int.Max(1, value);

        if (newPageSize == _pageSize) return;

        _pageSize = newPageSize;
        if (_initialized) DebounceRefreshMovieCards();
    }

    private void DebounceRefreshMovieCards()
    {
        _pageSizeRefreshDebounceTokenSource?.Cancel();
        var tokenSource = new CancellationTokenSource();
        _pageSizeRefreshDebounceTokenSource = tokenSource;
        _ = RefreshMovieCardsAfterDelayAsync(tokenSource);
    }

    private async Task RefreshMovieCardsAfterDelayAsync(CancellationTokenSource tokenSource)
    {
        try
        {
            await Task.Delay(300, tokenSource.Token);
            await Dispatcher.UIThread.InvokeAsync(RefreshMovieCardsAsync);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_pageSizeRefreshDebounceTokenSource, tokenSource))
            {
                _pageSizeRefreshDebounceTokenSource = null;
            }

            tokenSource.Dispose();
        }
    }

    [RelayCommand]
    private async Task SwitchMovieOrTv(string tag)
    {
        _isTagChanged2Refresh = false;
        DoubanTags.Clear();
        if (tag == "电影")
        {
            LoadMovieTags().ForEach(x => DoubanTags.Add(x));
            _switchMovieOrTv = "movie";
        }
        else if (tag == "电视")
        {
            LoadTvTags().ForEach(x => DoubanTags.Add(x));
            _switchMovieOrTv = "tv";
        }

        SelectedTagItem = DoubanTags.FirstOrDefault();

        if (_initialized || AppConifg.PlayerConfig.HomeAutoLoadDoubanEnabled)
            await RefreshMovieCardsAsync();
    }

    private List<string> LoadMovieTags()
    {
        if (!string.IsNullOrWhiteSpace(AppConifg.PlayerConfig.DoubanMovieTags))
        {
            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(AppConifg.PlayerConfig.DoubanMovieTags);
                if (tags is { Count: > 0 }) return tags;
            }
            catch { }
        }
        return [.. _defaultMovieTags];
    }

    private List<string> LoadTvTags()
    {
        if (!string.IsNullOrWhiteSpace(AppConifg.PlayerConfig.DoubanTvTags))
        {
            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(AppConifg.PlayerConfig.DoubanTvTags);
                if (tags is { Count: > 0 }) return tags;
            }
            catch { }
        }
        return [.. _defaultTvTags];
    }

    private void SaveMovieTags(List<string> tags)
    {
        AppConifg.PlayerConfig.DoubanMovieTags = JsonSerializer.Serialize(tags);
        SavePlayerConfig();
    }

    private void SaveTvTags(List<string> tags)
    {
        AppConifg.PlayerConfig.DoubanTvTags = JsonSerializer.Serialize(tags);
        SavePlayerConfig();
    }

    private static void SavePlayerConfig()
    {
        var playerConfigTable = App.Services.GetRequiredService<SugarRepository<PlayerConfig>>();
        if (AppConifg.PlayerConfig.Id > 0)
        {
            playerConfigTable.Update(AppConifg.PlayerConfig);
            return;
        }

        AppConifg.PlayerConfig.Id = playerConfigTable.Context.Insertable(AppConifg.PlayerConfig).ExecuteReturnIdentity();
    }

    private async Task RefreshMovieCardsAsync()
    {
        if (_isRefreshingMovieCards)
        {
            _refreshMovieCardsAgain = true;
            return;
        }

        _isRefreshingMovieCards = true;
        try
        {
            do
            {
                _refreshMovieCardsAgain = false;
                await RefreshMovieCardsCoreAsync();
            } while (_refreshMovieCardsAgain);
        }
        finally
        {
            _isRefreshingMovieCards = false;
        }
    }

    private string GetDoubanSubjectsUrl(string type, string tag, string sort, int limit, int start)
    {
        var encodedTag = Uri.EscapeDataString(tag);
        return $"https://movie.douban.com/j/search_subjects?type={type}&tag={encodedTag}&sort={sort}&page_limit={limit}&page_start={start}";
    }

    private async Task<string> FetchDoubanSubjectsInternal(string type, string tag, string sort, int limit, int start)
    {
#if !ANDROID
        if (_doubanVerifyWindow is not null)
        {
            var result = await _doubanVerifyWindow.FetchApiAsync(GetDoubanSubjectsUrl(type, tag, sort, limit, start));
            if (!string.IsNullOrWhiteSpace(result) && !result.StartsWith("<!DOCTYPE"))
            {
                _doubanVerifyWindow.HideAfterVerification();
                return result;
            }

            throw new InvalidOperationException("豆瓣验证窗口未返回有效数据，请确认验证已完成后再刷新。");
        }
#endif

        return await App.Services.GetRequiredService<IWebApi>()
            .FetchDoubanSubjectsByTag(type, tag, sort, limit, start);
    }

    private async Task<List<DoubanSubject>> SearchDoubanSubjectAsync(string searchText, string cat, CancellationToken cancellationToken = default)
    {
        // 方法1：通过HTML搜索页面获取结果（window.__DATA__）
        List<DoubanSubject>? htmlResults = null;
        try
        {
            htmlResults = await SearchDoubanFromHtmlPageAsync(searchText, cat, cancellationToken);
            if (htmlResults.Count > 0) return htmlResults;
            System.Diagnostics.Trace.WriteLine($"[DoubanSearch] HTML页面返回0条结果，尝试标签端点");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[DoubanSearch] HTML搜索页失败: {ex.GetType().Name}: {ex.Message}");
        }

        // 方法2：降级使用标签端点
        try
        {
            var sts = await FetchDoubanSubjectsInternal(cat, searchText, "recommend", 50, 0);
            var tagResults = ParseDoubanSubjects(sts);
            System.Diagnostics.Trace.WriteLine($"[DoubanSearch] 标签端点返回 {tagResults.Count} 条结果");
            return tagResults;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[DoubanSearch] 标签端点也失败: {ex.Message}");
            return htmlResults ?? [];
        }
    }

    private async Task<List<DoubanSubject>> SearchDoubanFromHtmlPageAsync(string searchText, string cat, CancellationToken cancellationToken = default)
    {
        var encodedText = Uri.EscapeDataString(searchText);
        var url = $"https://movie.douban.com/subject_search?search_text={encodedText}&cat={cat}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.Add("Referer", "https://movie.douban.com/");

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            AllowAutoRedirect = true
        };
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode == 403)
            throw new InvalidOperationException("豆瓣需要验证，请完成验证后重试。");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        System.Diagnostics.Trace.WriteLine($"[DoubanSearch] HTML页面长度: {html.Length}, 包含__DATA__: {html.Contains("window.__DATA__")}");
        var results = ParseDoubanSearchPageHtml(html);
        System.Diagnostics.Trace.WriteLine($"[DoubanSearch] HTML页面搜索 \"{searchText}\" 获取到 {results.Count} 条结果");
        return results;
    }

    private static List<DoubanSubject> ParseDoubanSearchPageHtml(string html)
    {
        const string marker = "window.__DATA__";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0) return [];

        var eqIndex = html.IndexOf('=', markerIndex + marker.Length);
        if (eqIndex < 0) return [];

        // Find the start of JSON (first '{' or '[' after '=')
        var jsonStart = -1;
        for (var i = eqIndex + 1; i < html.Length; i++)
        {
            if (html[i] is '{' or '[')
            {
                jsonStart = i;
                break;
            }
        }

        if (jsonStart < 0) return [];

        // Find matching closing brace/bracket
        var depth = 0;
        var inString = false;
        var escape = false;
        var openChar = html[jsonStart];
        var closeChar = openChar == '{' ? '}' : ']';

        for (var i = jsonStart; i < html.Length; i++)
        {
            var c = html[i];
            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == openChar) depth++;
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    var jsonStr = html[jsonStart..(i + 1)];
                    return ParseSearchPageItems(jsonStr);
                }
            }
        }

        return [];
    }

    private static List<DoubanSubject> ParseSearchPageItems(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // The JSON may be an array directly or an object with "items" property
            JsonElement itemsElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                itemsElement = root;
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var items))
            {
                itemsElement = items;
            }
            else
            {
                return [];
            }

            var results = new List<DoubanSubject>();
            foreach (var item in itemsElement.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(title)) continue;

                var coverUrl = item.TryGetProperty("cover_url", out var cu) ? cu.GetString() : null;
                var url = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                var id = item.TryGetProperty("id", out var idEl)
                    ? idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt64().ToString() : idEl.GetString()
                    : null;

                string? rate = null;
                if (item.TryGetProperty("rating", out var rating) && rating.ValueKind == JsonValueKind.Object &&
                    rating.TryGetProperty("value", out var value))
                {
                    rate = value.ValueKind == JsonValueKind.Number
                        ? value.GetDouble().ToString("0.#")
                        : value.GetString();
                }

                results.Add(new DoubanSubject
                {
                    Title = title,
                    Cover = NormalizeDoubanImageUrl(coverUrl),
                    Url = url ?? string.Empty,
                    Id = id ?? string.Empty,
                    Rate = rate
                });
            }

            return results;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            System.Diagnostics.Trace.WriteLine($"[DoubanSearch] HTML页面JSON解析失败: {ex.Message}");
            return [];
        }
    }


    private static bool IsDoubanVerificationRequired(Exception exception)
    {
        return exception is ApiException { StatusCode: HttpStatusCode.Forbidden }
               || (exception is InvalidOperationException { Message: var message }
                   && (message.Contains("豆瓣验证窗口未返回有效数据", StringComparison.Ordinal)
                       || message.Contains("豆瓣需要验证", StringComparison.Ordinal)));
    }

    private static string NormalizeDoubanImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;

        var normalized = imageUrl.Replace("\\/", "/").Trim();
        if (normalized.StartsWith("//")) normalized = $"https:{normalized}";
        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) normalized = "https://" + normalized[7..];

        return normalized.Replace("img2.doubanio.com", "img9.doubanio.com")
            .Replace("img3.doubanio.com", "img9.doubanio.com");
    }

    private static string GetImageCachePath(string imageUrl)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(imageUrl)));
        var extension = Path.GetExtension(Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ? uri.AbsolutePath : imageUrl);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var cachePath = Path.Combine(GlobalDefine.DataPath, "DoubanImages");
        if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
        return Path.Combine(cachePath, $"{hash}{extension}");
    }

    private static async Task<string> GetCachedDoubanImageAsync(string? imageUrl)
    {
        var normalized = NormalizeDoubanImageUrl(imageUrl);
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;

        var cacheFile = GetImageCachePath(normalized);
        if (File.Exists(cacheFile)) return cacheFile;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, normalized);
            request.Headers.TryAddWithoutValidation("User-Agent", LunaTV.Base.Constants.UserAgent.GetRandomUserAgent());
            request.Headers.TryAddWithoutValidation("Referer", "https://movie.douban.com/");
            request.Headers.TryAddWithoutValidation("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(cacheFile);
            await response.Content.CopyToAsync(fileStream);
            return cacheFile;
        }
        catch
        {
            return normalized;
        }
    }

    private static List<DoubanSubject> ParseDoubanSubjects(string json)
    {
        var trimmedJson = json.TrimStart();
        if (trimmedJson.Length == 0 || trimmedJson[0] is not ('{' or '[' or '"'))
            throw new InvalidOperationException("豆瓣验证窗口未返回有效数据，请确认验证已完成后再刷新。");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("豆瓣验证窗口未返回有效数据，请确认验证已完成后再刷新。");
        }

        if (root.ValueKind == JsonValueKind.String)
        {
            var innerJson = root.GetString();
            return string.IsNullOrWhiteSpace(innerJson) ? [] : ParseDoubanSubjects(innerJson);
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            try { return JsonSerializer.Deserialize<List<DoubanSubject>>(root.GetRawText(), DoubanJsonOptions) ?? []; }
            catch (JsonException ex) { System.Diagnostics.Trace.WriteLine($"[DoubanParse] JSON解析失败: {ex.Message}"); return []; }
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("subjects", out var subjectsElement) &&
            subjectsElement.ValueKind == JsonValueKind.Array)
        {
            try { return JsonSerializer.Deserialize<List<DoubanSubject>>(subjectsElement.GetRawText(), DoubanJsonOptions) ?? []; }
            catch (JsonException ex) { System.Diagnostics.Trace.WriteLine($"[DoubanParse] JSON解析失败: {ex.Message}"); return []; }
        }

        return [];
    }

    private async Task<List<DoubanSubject>> FetchDoubanSubjectsPageAsync(Action<List<DoubanSubject>>? onPageLoaded = null)
    {
        var subjects = new List<DoubanSubject>();
        var requests = int.Max(1, (int)Math.Ceiling((double)_pageSize / DoubanPageLimit));

        for (var i = 0; i < requests; i++)
        {
            try
            {
                var sts = await FetchDoubanSubjectsInternal(_switchMovieOrTv, SelectedTagItem!, "recommend", DoubanPageLimit,
                    _pageStart + i * DoubanPageLimit);
                var pageSubjects = ParseDoubanSubjects(sts);
                if (pageSubjects.Count == 0) break;
                subjects.AddRange(pageSubjects);
                onPageLoaded?.Invoke(pageSubjects);
                if (subjects.Count >= _pageSize) break;
            }
            catch (Exception e) when (subjects.Count > 0 && IsDoubanVerificationRequired(e))
            {
                break;
            }
        }

        return subjects.Take(_pageSize).ToList();
    }

    private async Task RefreshMovieCardsCoreAsync()
    {
        if (SelectedTagItem is null)
        {
            _isTagChanged2Refresh = true;
            return;
        }

        if (AppConifg.PlayerConfig.DoubanApiEnabled is false)
        {
            App.Notification?.Show(new Notification("温馨提示", "豆瓣接口未启动"),
                NotificationType.Information);
            _isTagChanged2Refresh = true;
            return;
        }

        if (_initialized) _ = Loading();

        try
        {
            var loadingClosed = false;
            var loadedCount = 0;
            MovieCardItems.Clear();

            var subjects = await FetchDoubanSubjectsPageAsync(pageSubjects =>
            {
                foreach (var item in pageSubjects)
                {
                    if (loadedCount >= _pageSize) return;

                    var card = new MovieCardItem
                    {
                        Name = item.Title,
                        Score = string.IsNullOrEmpty(item.Rate) ? "暂无" : item.Rate,
                        DoubanUrl = item.Url
                    };
                    MovieCardItems.Add(card);
                    _ = LoadMovieCardImageAsync(card, item.Cover);
                    loadedCount++;
                }

                if (!loadingClosed && MovieCardItems.Count > 0)
                {
                    _loadingWaitViewModel.Close();
                    IsLoading = false;
                    loadingClosed = true;
                }
            });

            if (subjects.Count == 0 && _pageStart > 0)
            {
                _pageStart = 0;
                loadedCount = 0;
                MovieCardItems.Clear();
                subjects = await FetchDoubanSubjectsPageAsync(pageSubjects =>
                {
                    foreach (var item in pageSubjects)
                    {
                        if (loadedCount >= _pageSize) return;

                        var card = new MovieCardItem
                        {
                            Name = item.Title,
                            Score = string.IsNullOrEmpty(item.Rate) ? "暂无" : item.Rate,
                            DoubanUrl = item.Url
                        };
                        MovieCardItems.Add(card);
                        _ = LoadMovieCardImageAsync(card, item.Cover);
                        loadedCount++;
                    }

                    if (!loadingClosed && MovieCardItems.Count > 0)
                    {
                        _loadingWaitViewModel.Close();
                        loadingClosed = true;
                    }
                });
            }
        }
        catch (Exception e)
        {
            if (IsDoubanVerificationRequired(e))
            {
                OpenDoubanVerifyWindow(true);
            }
            else
            {
                App.Notification?.Show(new Notification("查找失败", $"豆瓣检索失败：{e.Message}", NotificationType.Error), NotificationType.Error);
            }
        }


        if (_initialized)
            _loadingWaitViewModel.Close();
        IsLoading = false;
        _isTagChanged2Refresh = true;
    }

    private async Task LoadMovieCardImageAsync(MovieCardItem card, string? cover)
    {
        await _movieCardImageLoadSemaphore.WaitAsync();
        try
        {
            var image = await GetCachedDoubanImageAsync(cover);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (MovieCardItems.Contains(card)) card.Image = image;
            });
        }
        catch
        {
        }
        finally
        {
            _movieCardImageLoadSemaphore.Release();
        }
    }

    private void OpenDoubanVerifyWindow(bool showNotification)
    {
#if ANDROID
        try
        {
            var uri = global::Android.Net.Uri.Parse("https://movie.douban.com/");
            var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView, uri);
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            global::Android.App.Application.Context.StartActivity(intent);
            if (showNotification)
                App.Notification?.Show(new Notification("豆瓣需要验证", "已在系统浏览器中打开豆瓣，完成验证后返回应用即可。", NotificationType.Warning));
        }
        catch (Exception ex)
        {
            if (showNotification)
                App.Notification?.Show(new Notification("豆瓣需要验证", $"打开浏览器失败: {ex.Message}", NotificationType.Error));
        }
        return;
#else
        var owner = App.VisualRoot as MainWindow;
        if (_doubanVerifyWindow is not null)
        {
            if (showNotification)
                App.Notification?.Show(new Notification("豆瓣需要验证", "请在弹出的豆瓣窗口中手动完成验证，验证成功后窗口会自动隐藏。", NotificationType.Warning), NotificationType.Warning);

            _doubanVerifyWindow.WaitForVerification();
            _doubanVerifyWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _doubanVerifyWindow.Topmost = true;
            _doubanVerifyWindow.Show(owner);
            _doubanVerifyWindow.Activate();
            return;
        }

        if (showNotification)
            App.Notification?.Show(new Notification("豆瓣需要验证", "请在弹出的豆瓣窗口中手动完成验证，验证成功后窗口会自动隐藏。", NotificationType.Warning), NotificationType.Warning);

        _doubanVerifyWindow = new DoubanVerifyWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Topmost = true
        };
        _doubanVerifyWindow.VerificationCompleted += RefreshMovieCardsAsync;
        _doubanVerifyWindow.Closed += (_, _) =>
        {
            if (_doubanVerifyWindow is not null)
            {
                _doubanVerifyWindow.VerificationCompleted -= RefreshMovieCardsAsync;
                _doubanVerifyWindow = null;
            }
        };
        _doubanVerifyWindow.WaitForVerification();
        _doubanVerifyWindow.Show(owner);
        _doubanVerifyWindow.Activate();
#endif
    }

    partial void OnSelectedTagItemChanged(string? value)
    {
        _pageStart = 0;
        if (_isTagChanged2Refresh) _ = RefreshMovieCardsAsync();
    }

    [RelayCommand]
    private async Task ManageTags()
    {
        var isMovie = _switchMovieOrTv == "movie";
        var currentTags = isMovie ? LoadMovieTags() : LoadTvTags();

        var vm = new ManageDoubanTagsViewModel
        {
            DefaultMovieTags = [.. _defaultMovieTags],
            DefaultTvTags = [.. _defaultTvTags],
            IsMovieMode = isMovie
        };
        vm.LoadTags(currentTags);

        var options = new DialogOptions
        {
            Title = "",
            Mode = DialogMode.None,
            Button = DialogButton.OKCancel,
            ShowInTaskBar = false,
            IsCloseButtonVisible = true,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanDragMove = false,
            CanResize = false,
            StyleClass = ""
        };

        var result =
#if ANDROID
            await ShowManageTagsAsPage(vm);
#else
            await Dialog.ShowModal<ManageDoubanTagsView, ManageDoubanTagsViewModel>(vm, options: options);
#endif
        if (result == DialogResult.OK)
        {
            var tags = vm.GetTags();
            if (isMovie)
                SaveMovieTags(tags);
            else
                SaveTvTags(tags);

            // 刷新当前标签列表
            _isTagChanged2Refresh = false;
            var selectedTag = SelectedTagItem;
            DoubanTags.Clear();
            foreach (var tag in tags) DoubanTags.Add(tag);
            SelectedTagItem = DoubanTags.Contains(selectedTag ?? "") ? selectedTag : DoubanTags.FirstOrDefault();
            _isTagChanged2Refresh = true;
        }
    }

    [RelayCommand]
    private async Task MoreMovieOrTv()
    {
        _isTagChanged2Refresh = false;
        _pageStart += _pageSize;
        if (_pageStart > 9 * DoubanPageLimit) _pageStart = 0;

        await RefreshMovieCardsAsync();
    }

    [RelayCommand]
    private async Task BackHome()
    {
        MovieChecked = true;
        await SwitchMovieOrTv("电影");
    }

    [RelayCommand]
    private async void OpenLocalVideo()
    {
        var videoAll = new FilePickerFileType("All Videos")
        {
            Patterns = new string[6] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv" },
            AppleUniformTypeIdentifiers = new string[1] { "public.video" },
            MimeTypes = new string[1] { "video/*" }
        };


        var file = await App.StorageProvider?.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "打开文件",
                FileTypeFilter = new[]
                {
                    videoAll,
                    FilePickerFileTypes.All,
                },
                AllowMultiple = false,
            });

        if (file is { Count: > 0 })
        {
#if !ANDROID
            var win = new MpvPlayerWindow();
            (App.VisualRoot as MainWindow)?.Hide();
            win.Show();
            if (win.DataContext is MpvPlayerWindowModel videoModel)
            {
                var localPath = file[0].Path.LocalPath;
                var title = Path.GetFileName(localPath);
                videoModel.MediaUrl = localPath;
                videoModel.Title = title;
                videoModel.ViewHistory = new ViewHistory
                {
                    VodId = localPath,
                    Name = title,
                    Episode = title,
                    Url = localPath,
                    Source = "本地",
                    PlaybackPosition = 0,
                    Duration = 0,
                    TotalEpisodeCount = 1,
                    IsLocal = true
                };
            }
#else
            var localPath = file[0].Path.LocalPath;
            var title = Path.GetFileName(localPath);
            var viewHistory = new ViewHistory
            {
                VodId = localPath,
                Name = title,
                Episode = title,
                Url = localPath,
                Source = "本地",
                PlaybackPosition = 0,
                Duration = 0,
                TotalEpisodeCount = 1,
                IsLocal = true
            };
            AndroidVideoPlayerHelper.Play(localPath, title, viewHistory);
#endif
        }
    }

    [RelayCommand]
    private async Task NaviSearchDebounced(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _naviSearchDebounceCts?.Cancel();
        _naviSearchDebounceCts = new CancellationTokenSource();
        var cts = _naviSearchDebounceCts;
        try { await Task.Delay(400, cts.Token); }
        catch (OperationCanceledException) { return; }
        await NaviSearch(text);
    }

    [RelayCommand]
    private async Task NaviSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _naviSearchDebounceCts?.Cancel();

        if (AppConifg.PlayerConfig.DoubanApiEnabled is false)
        {
            var mvm = App.Services.GetRequiredService<MainViewModel>();
            var searchMenuItem = mvm.Items.FirstOrDefault(x => x.Name == "搜索");
            if (searchMenuItem is null) return;

            mvm.SelectedItem = searchMenuItem;
            if (mvm.GetControl(searchMenuItem.Name) is not TVShowSearchView { DataContext: TVShowSearchViewModel svm }) return;

            if (svm.IsSearching)
            {
                svm.StopCurrentSearch();
                var timeout = TimeSpan.FromSeconds(5);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (svm.IsSearching && sw.Elapsed < timeout)
                {
                    await Task.Delay(100);
                }
            }

            svm.InputMovieTvName = text;
            svm.IsAdultMode = false;
            await svm.Search(text);
            return;
        }

        _ = Loading();

        try
        {
            MovieCardItems.Clear();

            var subjects = await SearchDoubanSubjectAsync(text, _switchMovieOrTv);

            if (subjects.Count == 0)
            {
                App.Notification?.Show(new Notification("未找到", $"未找到与 \"{text}\" 相关的内容"), NotificationType.Information);
                return;
            }

            var cards = subjects.Select(item => new MovieCardItem
            {
                Name = string.IsNullOrWhiteSpace(item.Year) ? item.Title : $"{item.Title} ({item.Year})",
                Image = null,
                Score = string.IsNullOrEmpty(item.Rate) ? "暂无" : item.Rate,
                DoubanUrl = item.Url
            }).ToList();

            foreach (var card in cards)
                MovieCardItems.Add(card);

            _ = Task.WhenAll(cards.Select(async (card, i) =>
            {
                try
                {
                    card.Image = await GetCachedDoubanImageAsync(subjects[i].Cover);
                }
                catch { }
            }));
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            if (IsDoubanVerificationRequired(ex))
            {
                OpenDoubanVerifyWindow(true);
            }
            else
            {
                App.Notification?.Show(new Notification("查找失败", $"豆瓣检索失败：{ex.Message}"), NotificationType.Error);
            }
        }
        finally
        {
            _loadingWaitViewModel.Close();
            IsLoading = false;
        }
    }

    public async Task Loading()
    {
#if ANDROID
        // Dialog.ShowModal creates a Window which isn't supported on Android single-view mode
        IsLoading = true;
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

#if ANDROID
    private async Task<DialogResult> ShowManageTagsAsPage(ManageDoubanTagsViewModel vm)
    {
        var mainViewModel = App.Services.GetRequiredService<MainViewModel>();
        var prevPage = mainViewModel.PageContent;

        var view = new ManageDoubanTagsView { DataContext = vm };

        var panel = new Avalonia.Controls.StackPanel();
        var btnPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 10),
            Spacing = 20
        };

        var tcs = new TaskCompletionSource<DialogResult>();
        var confirmBtn = new Avalonia.Controls.Button { Content = "确认", MinWidth = 100 };
        var cancelBtn = new Avalonia.Controls.Button { Content = "取消", MinWidth = 100 };
        confirmBtn.Click += (_, _) => { mainViewModel.PageContent = prevPage; tcs.TrySetResult(DialogResult.OK); };
        cancelBtn.Click += (_, _) => { mainViewModel.PageContent = prevPage; tcs.TrySetResult(DialogResult.None); };
        btnPanel.Children.Add(confirmBtn);
        btnPanel.Children.Add(cancelBtn);

        panel.Children.Add(view);
        panel.Children.Add(btnPanel);
        var wrapper = new Avalonia.Controls.UserControl
        {
            Content = new Avalonia.Controls.ScrollViewer { Content = panel, Padding = new Avalonia.Thickness(20) }
        };
        mainViewModel.PageContent = wrapper;

        return await tcs.Task;
    }
#endif
}

public partial class MovieCardItem : ViewModelBase
{
    [ObservableProperty] private string? _doubanUrl;

    [ObservableProperty] private string? _image;
    [ObservableProperty] private string? _name;

    [ObservableProperty] private string? _score;


    [RelayCommand]
    private void OpenDoubanUrl()
    {
        if (string.IsNullOrWhiteSpace(DoubanUrl)) return;
#if !ANDROID
        var window = new WebBrowserWindow(DoubanUrl);
        window.Show();
#else
        try
        {
            var uri = global::Android.Net.Uri.Parse(DoubanUrl);
            var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView, uri);
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            global::Android.App.Application.Context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            App.Notification?.Show(new Notification("提示", $"打开浏览器失败: {ex.Message}", NotificationType.Error));
        }
#endif
    }

    [RelayCommand]
    private async Task Search(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var mvm = App.Services.GetRequiredService<MainViewModel>();
        var searchMenuItem = mvm.Items.FirstOrDefault(x => x.Name == "搜索");
        if (searchMenuItem is null) return;

        mvm.SelectedItem = searchMenuItem;
        if (mvm.GetControl(searchMenuItem.Name) is not TVShowSearchView { DataContext: TVShowSearchViewModel svm }) return;

        if (svm.IsSearching)
        {
            svm.StopCurrentSearch();
            var timeout = TimeSpan.FromSeconds(5);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (svm.IsSearching && sw.Elapsed < timeout)
            {
                await Task.Delay(100);
            }
        }

        svm.InputMovieTvName = name;
        svm.IsAdultMode = false;
        await svm.Search(name);
    }
}
