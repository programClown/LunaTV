using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Base.DB;
using LunaTV.Constants;
using LunaTV.Services;
using LunaTV.ViewModels;
using LunaTV.ViewModels.TVShowPages;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace LunaTV.Extensions;

/// <summary>
///     依赖注入
/// </summary>
public static class ServiceCollectionExtenstion
{
    /// <summary>
    ///     注入通用服务
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddServices(this IServiceCollection serviceCollection)
    {
        // 影视资源查找
        serviceCollection.AddScoped<MovieTvService>();

        //影视资源
        // Configure Refit and Resilience
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        var defaultRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
        };

        // Refit settings for IApiFactory
        var defaultSystemTextJsonSettings = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        defaultSystemTextJsonSettings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        var apiFactoryRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(defaultSystemTextJsonSettings),
            ExceptionFactory = async response =>
            {
                if (!response.IsSuccessStatusCode)
                    // var error = await response.Content.ReadAsStringAsync();
                {
                    Console.WriteLine($"API 错误: {response.StatusCode}");
                }

                return null;
            }
        };

        // Add Refit client factory
        serviceCollection
            .AddSingleton<IApiFactory, ApiFactory>(provider =>
                new ApiFactory(
                    provider.GetRequiredService<IHttpClientFactory>()
                )
                {
                    RefitSettings = apiFactoryRefitSettings
                })
            .ConfigureHttpClientDefaults(config => config.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    AllowAutoRedirect = true
                })
            );

        serviceCollection
            .AddRefitClient<IWebApi>(defaultRefitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://movie.douban.com");
                c.Timeout = TimeSpan.FromHours(1);
                c.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
                c.DefaultRequestHeaders.Add("User-Agent", UserAgent.GetRandomUserAgent());
                c.DefaultRequestHeaders.Add("Referer", "https://movie.douban.com/");
                c.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
                c.Timeout = TimeSpan.FromSeconds(20);
            })
            .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30); // 总的超时时间
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15); //每次重试的超时时间
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60); //熔断时间
                }
            );

        // IMovieTvApi named HttpClient — bounded per-request timeout without a shared circuit breaker.
        // Source-level health is tracked by MovieTvService so one flaky source won't open a
        // global breaker for every source using the same Refit interface.
        serviceCollection
            .AddHttpClient(nameof(IMovieTvApi), client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(12);
                }
            );

        serviceCollection.AddSingleton<AppJsonConfigService>();
    }


    /// <summary>
    ///     注入 View Model
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddViewModels(this IServiceCollection serviceCollection)
    {
        // page view model
        serviceCollection.AddTransient<SettingsViewModel>();
        serviceCollection.AddSingleton<TVShowHistoryViewModel>();
        serviceCollection.AddSingleton<MainViewModel>();
        serviceCollection.AddSingleton<TVDownloadViewModel>();
    }

    /// <summary>
    ///     注入页面（Views）
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddViews(this IServiceCollection serviceCollection)
    {
        // 主窗口 (desktop only — not resolved on Android)
#if !ANDROID
        serviceCollection.AddSingleton<MainWindow>();
#endif
        serviceCollection.AddSingleton<MainView>();
        serviceCollection.AddSingleton<TVDownloadView>(provider =>
            new TVDownloadView
            {
                DataContext = provider.GetRequiredService<TVDownloadViewModel>()
            });
        serviceCollection.AddTransient<SettingsView>(provider =>
            new SettingsView
            {
                DataContext = provider.GetRequiredService<SettingsViewModel>()
            });
        serviceCollection.AddSingleton<TVShowHistoryView>(provider =>
            new TVShowHistoryView
            {
                DataContext = provider.GetRequiredService<TVShowHistoryViewModel>()
            });
    }

    /// <summary>
    ///     注入数据库（DB）
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddDb(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqlSugarClient(GlobalDefine.DbConn);
        serviceCollection.AddSugarRepository();
    }
}