using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    /// 搜索
    /// </summary>
    /// <param name="source"><see cref="ApiSourceInfo.ApiSitesConfig"/>网站源</param>
    /// <returns></returns>
    public async Task<List<SearchResult>> Search(string source, string name)
    {
        var searchResults = new List<SearchResult>();

        var site = ApiSourceInfo.ApiSitesConfig[source];
        try
        {
            var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.ApiBaseUrl));
            var results = await apiService.SearchVideos(name);
            var json = JsonSerializer.Deserialize<MovieSoubject>(results,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // 处理大小写不敏感
                });
            // Console.WriteLine(json);
            if (json is { List.Count: > 0 })
            {
                json.List.ForEach(x =>
                {
                    searchResults.Add(new SearchResult()
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
                        ApiUrlAttr = site.ApiBaseUrl,
                    });
                });
            }

            var pageCount = json.PageCount;
            // 确定需要获取的额外页数 (最多获取maxPages页)
            var pagesToFetch = Math.Min(pageCount - 1, AppConifg.SearchMaxPages - 1);

            for (var i = 2; i <= pagesToFetch + 1; i++)
            {
                var pageResults = await apiService.PageSearchVideos(name, i);
                var pageJson = JsonSerializer.Deserialize<MovieSoubject>(pageResults,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // 处理大小写不敏感
                    });
                if (pageJson is { List.Count: > 0 })
                {
                    pageJson.List.ForEach(x =>
                    {
                        searchResults.Add(new SearchResult()
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
                            ApiUrlAttr = site.ApiBaseUrl,
                        });
                    });
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return searchResults;
    }

    public async Task<DetailResult> SearchDetail(string source, string vodId)
    {
        var site = ApiSourceInfo.ApiSitesConfig[source];
        string results;
        if (string.IsNullOrEmpty(site.DetailBaseUrl))
        {
            var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.ApiBaseUrl));
            results = await apiService.GetVideoDetail(vodId);

            var json = JsonSerializer.Deserialize<MovieSoubject>(results,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // 处理大小写不敏感
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
                            .Select(parts => parts[1]) // 提取 URL
                    )
                    .ToList();
            }
        }
        else
        {
            var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.DetailBaseUrl));
            results = await apiService.GetSpecialSourceVideoDetail(vodId);
        }

        return null;
    }
}