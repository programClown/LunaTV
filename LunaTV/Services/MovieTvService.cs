using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Constants;
using LunaTV.Models;

namespace LunaTV.Services;

public class MovieTvService
{
    private readonly IApiFactory _apiFactory;
    private readonly ConcurrentDictionary<string, string> _lastSearchPageErrors = new();

    public string? LastSearchPageError { get; private set; }

    public string? GetLastSearchPageError(string source)
    {
        return _lastSearchPageErrors.TryGetValue(source, out var error) ? error : null;
    }

    public MovieTvService(IApiFactory apiFactory)
    {
        _apiFactory = apiFactory;
    }

    private static readonly ConcurrentDictionary<string, DateTime> s_deadHosts = new();
    private static readonly TimeSpan DeadHostTtl = TimeSpan.FromMinutes(5);

    public static bool IsHostDead(string source)
    {
        if (s_deadHosts.TryGetValue(source, out var failureTime))
        {
            if (DateTime.UtcNow - failureTime < DeadHostTtl)
                return true;
            s_deadHosts.TryRemove(source, out _);
        }

        return false;
    }

    private static void MarkHostDead(string source)
    {
        s_deadHosts[source] = DateTime.UtcNow;
    }

    private static void MarkHostAlive(string source)
    {
        s_deadHosts.TryRemove(source, out _);
    }

    private static string NormalizeCoverUrl(string? cover, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(cover)) return string.Empty;

        var normalized = cover.Replace("\\/", "/").Trim();
        if (normalized.StartsWith("//")) return $"https:{normalized}";
        if (Uri.TryCreate(normalized, UriKind.Absolute, out _)) return normalized;

        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) && Uri.TryCreate(baseUri, normalized, out var absoluteUri)
            ? absoluteUri.ToString()
            : normalized;
    }

    private static int ParseYear(string? year)
    {
        return int.TryParse(year, out var value) ? value : 0;
    }

    private static Uri GetSiteRootUri(string baseUrl)
    {
        var uri = new Uri(baseUrl);
        return new Uri(uri.GetLeftPart(UriPartial.Authority));
    }

    private static string NormalizeEpisodeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        var normalized = WebUtility.HtmlDecode(url)
            .Replace("\\/", "/")
            .Trim()
            .Trim('\'', '"');

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)) return string.Empty;
        return uri.Scheme is "http" or "https" ? uri.ToString() : string.Empty;
    }

    private static bool IsPlayableEpisodeUrl(string? url)
    {
        var normalized = NormalizeEpisodeUrl(url);
        if (string.IsNullOrEmpty(normalized)) return false;

        return normalized.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith(".flv", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains(".m3u8?", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains(".mp4?", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains(".mkv?", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains(".flv?", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains(".ts?", StringComparison.OrdinalIgnoreCase);
    }

    private static EpisodeSubject? CreateEpisode(string? name, string? url)
    {
        var normalizedUrl = NormalizeEpisodeUrl(url);
        if (!IsPlayableEpisodeUrl(normalizedUrl)) return null;

        return new EpisodeSubject
        {
            Name = string.IsNullOrWhiteSpace(name) ? "正片" : WebUtility.HtmlDecode(name).Trim(),
            Url = normalizedUrl
        };
    }

    private static List<SearchResult> MapSearchResults(IEnumerable<MovieSubSubject> subjects, string source, ApiSourceInfo site)
    {
        return subjects.Select(x => new SearchResult
        {
            Id = x.VodId,
            Source = source,
            SourceName = site.Name,
            Name = x.VodName ?? string.Empty,
            Tag = x.TypeName ?? string.Empty,
            Year = ParseYear(x.VodYear),
            Cover = NormalizeCoverUrl(x.VodPic, site.ApiBaseUrl),
            Descriptor = x.VodContent ?? string.Empty,
            ReMark = x.VodRemarks ?? "暂无介绍",
            ApiUrlAttr = site.ApiBaseUrl
        }).ToList();
    }

    public async Task<(List<SearchResult> Results, int PageCount)> SearchPage(string source, string name, int page, bool isAdult = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LastSearchPageError = null;
            _lastSearchPageErrors.TryRemove(source, out _);
            if (IsHostDead(source))
                return ([], 0);
            var site = isAdult ? AppConifg.AdultApiSitesConfig[source] : AppConifg.ApiSitesConfig[source];
            var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(GetSiteRootUri(site.ApiBaseUrl));
            var results = page <= 1
                ? await apiService.SearchVideos(name, cancellationToken)
                : await apiService.PageSearchVideos(name, page, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var json = JsonSerializer.Deserialize<MovieSubject>(results,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            MarkHostAlive(source);
            return json is { List.Count: > 0 }
                ? (MapSearchResults(json.List, source, site), json.PageCount)
                : ([], json?.PageCount ?? 0);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            MarkHostDead(source);
            LastSearchPageError = e.Message;
            _lastSearchPageErrors[source] = e.Message;
            System.Diagnostics.Trace.WriteLine(e);
            return ([], 0);
        }
    }

    /// <summary>
    ///     搜索
    /// </summary>
    /// <param name="source"><see cref="ApiSourceInfo.ApiSitesConfig" />网站源</param>
    /// <returns></returns>
    public async Task<List<SearchResult>> Search(string source, string name, bool isAdult = false)
    {
        var searchResults = new List<SearchResult>();

        try
        {
            var (results, pageCount) = await SearchPage(source, name, 1, isAdult);
            searchResults.AddRange(results);
            var pagesToFetch = Math.Min(pageCount - 1, AppConifg.SearchMaxPages - 1);

            for (var i = 2; i <= pagesToFetch + 1; i++)
            {
                var (pageResults, _) = await SearchPage(source, name, i, isAdult);
                searchResults.AddRange(pageResults);
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.WriteLine(e);
        }

        return searchResults;
    }

    public async Task<DetailResult?> SearchDetail(string source, string vodId, bool isAdult = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var site = isAdult ? AppConifg.AdultApiSitesConfig[source] : AppConifg.ApiSitesConfig[source];
            if (AppConifg.PlayerConfig.ForceApiNeedSpecialSource || string.IsNullOrEmpty(site.DetailBaseUrl))
            {
                var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(GetSiteRootUri(site.ApiBaseUrl));
                var results = await apiService.GetVideoDetail(vodId, cancellationToken);

                var json = JsonSerializer.Deserialize<MovieSubject>(results,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 处理大小写不敏感
                    });
                if (json is { List.Count: > 0 })
                {
                    var videoDetail = json.List[0];
                    var detailResult = new DetailResult();
                    var episodes = videoDetail.VodPlayUrl?
                        .Split("$$$", StringSplitOptions.RemoveEmptyEntries) // 分割播放源
                        .Select(mainSource => mainSource
                            .Split("#", StringSplitOptions.RemoveEmptyEntries) // 分割剧集
                            .Select(episodeItem => episodeItem.Split('$')) // 分割剧集信息
                            .Where(parts => parts.Length > 1)
                            .Select(parts => CreateEpisode(parts[0], parts[1]))
                            .Where(episode => episode is not null)
                            .Select(episode => episode!)
                            .ToList())
                        .FirstOrDefault(sourceEpisodes => sourceEpisodes.Count > 0) ?? [];
                    if (episodes.Count == 0 && !string.IsNullOrEmpty(videoDetail.VodContent))
                    {
                        var urls = Regex.Matches(videoDetail.VodContent, AppConifg.M3U8_PATTERN)
                            .Select(m => NormalizeEpisodeUrl(m.Value))
                            .Where(IsPlayableEpisodeUrl)
                            .ToList();
                        episodes.AddRange(urls.Select((x, i) => new EpisodeSubject { Name = $"第{i + 1}集", Url = x }));
                    }

                    MarkHostAlive(source);
                    return new DetailResult
                    {
                        VodId = vodId,
                        Episodes = episodes,
                        DetailUrl = site.ApiBaseUrl,
                        Title = json.List[0].VodName,
                        Cover = json.List[0].VodPic,
                        Desc = json.List[0].VodContent,
                        Type = json.List[0].TypeName,
                        Year = json.List[0].VodYear,
                        Area = json.List[0].VodArea,
                        Director = json.List[0].VodDirector,
                        Actor = json.List[0].VodActor,
                        Remark = json.List[0].VodRemarks,
                        Source = source,
                        SourceName = site.IsCustomApi ? $"自定义源-{site.Name}" : site.Name
                    };
                }

                return new DetailResult
                {
                    VodId = vodId,
                    DetailUrl = site.ApiBaseUrl,
                    Source = source,
                    SourceName = site.IsCustomApi ? $"自定义源-{site.Name}" : site.Name
                };
            }
            else
            {
                var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.DetailBaseUrl));
                var results = await apiService.GetSpecialSourceVideoDetail(vodId, cancellationToken);

                // 使用通用模式提取m3u8链接
                var matches = new List<string>();
                string generalPattern;
                if (source.Equals("ffzy"))
                {
                    generalPattern = @"\$(https?:\/\/[^""'\s]+?\/\d{8}\/\d+_[a-f0-9]+\/index\.m3u8)";
                    matches = Regex.Matches(results, generalPattern)
                        .Select(m => m.Groups[1].Value)
                        .ToList();
                }

                if (matches.Count == 0)
                {
                    generalPattern = @"\$(https?:\/\/[^""'\s]+?\.m3u8)";
                    matches = Regex.Matches(results, generalPattern)
                        .Select(m => m.Groups[1].Value) // 提取捕获组
                        .ToList();
                }

                var urls = new HashSet<string>(matches
                    .Select(NormalizeEpisodeUrl)
                    .Where(IsPlayableEpisodeUrl));
                //下边这个查找非常不准
                // var titleMatch = Regex.Matches(results, @"<h1[^>]*>(.*?)<\/h1>")
                //     .Select(m => m.Groups[1].Value) // 提取捕获组
                //     .ToList();
                // var titleText = titleMatch.Count < 2 ? "" : titleMatch[1].Trim();
                // var descMatch = Regex.Matches(results, @"<div[^>]*class=[""']sketch[""'][^>]*>([\s\S]*?)<\/div>")
                //     .Select(m => m.Groups[1].Value) // 提取捕获组
                //     .ToList();
                // var descText = descMatch.Count < 2 ? "" : Regex.Replace(descMatch[1], @"<[^>]+>", " ");
                var episodes = urls.Select((url, i) => new EpisodeSubject
                {
                    Name = $"第{i + 1}集",
                    Url = url
                }).ToList();

                if (episodes.Count > 0) MarkHostAlive(source);
                return new DetailResult
                {
                    VodId = vodId,
                    Episodes = episodes,
                    DetailUrl = site.DetailBaseUrl,
                    Source = source,
                    SourceName = site.IsCustomApi ? $"自定义源-{site.Name}" : site.Name
                };
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            MarkHostDead(source);
            System.Diagnostics.Trace.WriteLine(e);
        }


        return null;
    }
}