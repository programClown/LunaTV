using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Messaging;
using LunaTV.Base.Api;
using LunaTV.Base.DB;
using LunaTV.Constants;
using LunaTV.ViewModels;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Timeout;
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
        // 主窗口
        serviceCollection.AddSingleton<MainWindow>();
        serviceCollection.AddSingleton<MainView>();
        serviceCollection.AddSingleton<Lazy<MainWindow>>(provider =>
            new Lazy<MainWindow>(provider.GetRequiredService<MainWindow>));
        serviceCollection.AddSingleton<Lazy<MainView>>(provider =>
            new Lazy<MainView>(provider.GetRequiredService<MainView>));
        serviceCollection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        //影视资源
        // Configure Refit and Polly
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        var defaultRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
        };

        // Refit settings for IApiFactory
        var defaultSystemTextJsonSettings = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        defaultSystemTextJsonSettings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        var apiFactoryRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(defaultSystemTextJsonSettings),
        };

        // HTTP Policies
        var retryStatusCodes = new[]
        {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout, // 504
        };

        // Default retry policy: ~30s max
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(r => retryStatusCodes.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                Backoff.DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromMilliseconds(750),
                    retryCount: 6
                ),
                onRetry: (result, timeSpan, retryCount, _) =>
                {
                    if (retryCount > 3)
                    {
                        Debug.WriteLine(
                            "Retry attempt {Count}/{Max} after {Seconds:N2}s due to ({Status}) {Msg}",
                            retryCount,
                            6,
                            timeSpan.TotalSeconds,
                            result?.Result?.StatusCode,
                            result?.Result?.ToString()
                        );
                    }
                }
            )
            // 10s timeout for each attempt
            .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(60)));

        // Longer retry policy: ~60s max
        var retryPolicyLonger = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(r => retryStatusCodes.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                Backoff.DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromMilliseconds(1000),
                    retryCount: 7
                ),
                onRetry: (result, timeSpan, retryCount, _) =>
                {
                    if (retryCount > 4)
                    {
                        Debug.WriteLine(
                            "Retry attempt {Count}/{Max} after {Seconds:N2}s due to ({Status}) {Msg}",
                            retryCount,
                            7,
                            timeSpan.TotalSeconds,
                            result?.Result?.StatusCode,
                            result?.Result?.ToString()
                        );
                    }
                }
            )
            // 30s timeout for each attempt
            .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(120)));

        // Shorter local retry policy: ~5s total
        var localRetryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(r => retryStatusCodes.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                Backoff.DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromMilliseconds(320),
                    retryCount: 5
                )
            )
            // 3s timeout for each attempt
            .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3)));

        // Add Refit client factory
        serviceCollection.AddSingleton<IApiFactory, ApiFactory>(provider => new ApiFactory(
            provider.GetRequiredService<IHttpClientFactory>()
        )
        {
            RefitSettings = apiFactoryRefitSettings,
        });

        serviceCollection
            .AddRefitClient<IWebApi>(defaultRefitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://movie.douban.com");
                c.Timeout = TimeSpan.FromHours(1);
            })
            .AddPolicyHandler(retryPolicyLonger);
    }


    /// <summary>
    ///     注入 View Model
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddViewModels(this IServiceCollection serviceCollection)
    {
        // page view model
        serviceCollection.AddTransient<TVShowViewModel>();
        serviceCollection.AddTransient<InnovationPlazaViewModel>();
        serviceCollection.AddTransient<SettingsViewModel>();
        serviceCollection.AddTransient<PlaygroundViewModel>();
        serviceCollection.AddSingleton<MainViewModel>(provider =>
            new MainViewModel
            {
                Pages =
                {
                    provider.GetRequiredService<TVShowViewModel>(),
                    provider.GetRequiredService<InnovationPlazaViewModel>(),
                    provider.GetRequiredService<PlaygroundViewModel>()
                },
                FooterPages =
                {
                    provider.GetRequiredService<SettingsViewModel>()
                }
            }
        );
    }

    /// <summary>
    ///     注入页面（Views）
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddViews(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<TVShowView>();
        serviceCollection.AddTransient<InnovationPlazaView>();
        serviceCollection.AddSingleton<SettingsView>();
        serviceCollection.AddSingleton<PlaygroundView>();
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