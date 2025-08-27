using System.Collections.Generic;
using LunaTV.Base.Constants;
using LunaTV.Base.Models;

namespace LunaTV.Constants;

public static class AppConifg
{
    public const int SearchMaxPages = 50;
    public const string M3U8_PATTERN = @"https?:\/\/[^""'\s]+?\.m3u8";

    public const int SearchMaxVideos = 1000; //最多搜索多少部资源
    public static readonly List<string> SelectApis = ["dyttzy", "tyyszy"];
    public static readonly List<string> SelectAdultApis = [];

    public static Dictionary<string, ApiSourceInfo> ApiSitesConfig = new();
    public static Dictionary<string, ApiSourceInfo> AdultApiSitesConfig = new();
    public static PlayerConfig PlayerConfig = new();

    public static void UpdateSites(List<ApiSource> apiSources)
    {
        ApiSitesConfig.Clear();
        AdultApiSitesConfig.Clear();

        foreach (var apiSource in apiSources)
        {
            if (apiSource.IsAdult)
            {
                AppConifg.AdultApiSitesConfig.Add(apiSource.Source, new ApiSourceInfo(
                    ApiBaseUrl: apiSource.ApiBaseUrl,
                    DetailBaseUrl: apiSource.DetailBaseUrl,
                    IsCustomApi: apiSource.IsCustomApi,
                    IsAdult: apiSource.IsAdult,
                    Name: apiSource.Name
                ));
            }
            else
            {
                AppConifg.ApiSitesConfig.Add(apiSource.Source, new ApiSourceInfo(ApiBaseUrl: apiSource.ApiBaseUrl,
                    DetailBaseUrl: apiSource.DetailBaseUrl,
                    IsCustomApi: apiSource.IsCustomApi,
                    IsAdult: apiSource.IsAdult,
                    Name: apiSource.Name
                ));
            }
        }
    }
}