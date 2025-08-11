using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSearchViewModel : ViewModelBase
{
    public ObservableCollection<string> HistoryMovies { get; set; }
    public List<string> SelectApis { get; set; } = ["dyttzy", "tyyszy"];
    public ObservableCollection<SearchResult> SearchResults { get; set; }

    [ObservableProperty] private string? _inputMovieTvName;
    [ObservableProperty] private string? _searchCountText = "0个结果";
    private readonly IApiFactory _apiFactory;

    public TVShowSearchViewModel()
    {
        _apiFactory = App.Services.GetRequiredService<IApiFactory>();
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
            var site = ApiSourceInfo.ApiSitesConfig[api];
            try
            {
                var apiService = _apiFactory.CreateRefitClient<IMovieTvApi>(new Uri(site.ApiBaseUrl));
                var results = await apiService.SearchVideos(name);
                var json = JsonSerializer.Deserialize<MovieSoubject>(results,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // 处理大小写不敏感
                    });
                Console.WriteLine(json);
                if (json is { List.Count: > 0 })
                {
                    json.List.ForEach(x =>
                    {
                        SearchResults.Add(new SearchResult()
                        {
                            Id = x.VodId,
                            Source = api,
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
                            SearchResults.Add(new SearchResult()
                            {
                                Id = x.VodId,
                                Source = api,
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
        var site = ApiSourceInfo.ApiSitesConfig[apiKey];
        if (!string.IsNullOrEmpty(site.DetailPath)) //有detial
        {
            var detail = _apiFactory
                .CreateRefitClient<IMovieTvApi>(new Uri(site.DetailBaseUrl));
            try
            {
                var html = await detail.GetSpecialSourceVideoDetail(vodId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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
        App.Notification.Show(
            new Notification("播放", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);
    }
}

public class SearchResult
{
    public string Id { get; set; } //vod_id
    public string Source { get; set; } = string.Empty; //网站源
    public string SourceName { get; set; } = string.Empty; //网站源名称
    public string Name { get; set; } = string.Empty; //电影名称
    public string Tag { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Cover { get; set; } = string.Empty;
    public string Descriptor { get; set; } = string.Empty;
    public string ReMark { get; set; } = string.Empty;
    public string ApiUrlAttr { get; set; } = string.Empty;
}