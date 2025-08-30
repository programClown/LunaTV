using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Constants;
using LunaTV.Models;

namespace LunaTV.Services;

public class MovieTvService
{
    private readonly IApiFactory _apiFactory;

    public MovieTvService(IApiFactory apiFactory)
    {
        _apiFactory = apiFactory;
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
            var site = isAdult ? AppConifg.AdultApiSitesConfig[source] : AppConifg.ApiSitesConfig[source];
            var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.ApiBaseUrl));
            var results = await apiService.SearchVideos(name);
            var json = JsonSerializer.Deserialize<MovieSoubject>(results,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // 处理大小写不敏感
                });
            // Console.WriteLine(json);
            if (json is { List.Count: > 0 })
                json.List.ForEach(x =>
                {
                    searchResults.Add(new SearchResult
                    {
                        Id = x.VodId,
                        Source = source,
                        SourceName = site.Name,
                        Name = x.VodName,
                        Tag = x.TypeName,
                        Year = int.Parse(x.VodYear),
                        Cover = x.VodPic,
                        Descriptor = x.VodContent,
                        ReMark = x.VodRemarks ?? "暂无介绍",
                        ApiUrlAttr = site.ApiBaseUrl
                    });
                });

            var pageCount = json.PageCount;
            // 确定需要获取的额外页数 (最多获取maxPages页)
            var pagesToFetch = Math.Min(pageCount - 1, AppConifg.SearchMaxPages - 1);

            for (var i = 2; i <= pagesToFetch + 1; i++)
            {
                var pageResults = await apiService.PageSearchVideos(name, i);
                var pageJson = JsonSerializer.Deserialize<MovieSoubject>(pageResults,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 处理大小写不敏感
                    });
                if (pageJson is { List.Count: > 0 })
                    pageJson.List.ForEach(x =>
                    {
                        searchResults.Add(new SearchResult
                        {
                            Id = x.VodId,
                            Source = source,
                            SourceName = site.Name,
                            Name = x.VodName,
                            Tag = x.TypeName,
                            Year = int.Parse(x.VodYear),
                            Cover = x.VodPic,
                            Descriptor = x.VodContent,
                            ReMark = x.VodRemarks ?? "暂无介绍",
                            ApiUrlAttr = site.ApiBaseUrl
                        });
                    });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return searchResults;
    }

    public async Task<DetailResult?> SearchDetail(string source, string vodId, bool isAdult = false)
    {
        try
        {
            var site = isAdult ? AppConifg.AdultApiSitesConfig[source] : AppConifg.ApiSitesConfig[source];
            if (AppConifg.PlayerConfig.ForceApiNeedSpecialSource || string.IsNullOrEmpty(site.DetailBaseUrl))
            {
                var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.ApiBaseUrl));
                var results = await apiService.GetVideoDetail(vodId);

                var json = JsonSerializer.Deserialize<MovieSoubject>(results,
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
                        .Take(1) // 只取第一个播放源
                        .SelectMany(mainSource => mainSource
                                .Split("#", StringSplitOptions.RemoveEmptyEntries) // 分割剧集
                                .Select(episodeItem => episodeItem.Split('$')) // 分割剧集信息
                                .Where(parts => parts.Length > 1 &&
                                                (parts[1].StartsWith("http://") ||
                                                 parts[1].StartsWith("https://"))) // 检查合法 URL
                                .Select(parts =>
                                {
                                    return new EpisodeSubject
                                    {
                                        Name = parts[0],
                                        Url = parts[1]
                                    };
                                }) // 提取 URL
                        )
                        .ToList();
                    if (episodes.Count == 0 && !string.IsNullOrEmpty(videoDetail.VodContent))
                    {
                        var urls = Regex.Matches(videoDetail.VodContent, AppConifg.M3U8_PATTERN)
                            .Select(m => m.Value)
                            .ToList();
                        episodes.AddRange(urls.Select(x => new EpisodeSubject { Name = "", Url = x }));
                    }

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
                var results = await apiService.GetSpecialSourceVideoDetail(vodId);

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

                var urls = new HashSet<string>(matches);
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
        catch (Exception e)
        {
            Console.WriteLine(e);
        }


        return null;
    }
}