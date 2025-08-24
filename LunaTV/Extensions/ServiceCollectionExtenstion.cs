using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Messaging;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Base.DB;
using LunaTV.Constants;
using LunaTV.Services;
using LunaTV.ViewModels;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Notification = Ursa.Controls.Notification;

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

        // 影视资源查找
        serviceCollection.AddScoped<MovieTvService>();

        //影视资源
        // Configure Refit and Resilience
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
            ExceptionFactory = async (response) =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    // var error = await response.Content.ReadAsStringAsync();
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
                    RefitSettings = apiFactoryRefitSettings,
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
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5); //每次重试的超时时间
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30); //熔断时间
                }
            );
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