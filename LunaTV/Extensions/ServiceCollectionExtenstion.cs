using System;
using CommunityToolkit.Mvvm.Messaging;
using LunaTV.Services;
using LunaTV.Services.Impl;
using LunaTV.ViewModels;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;

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
        serviceCollection.AddSingleton<INavigationService, DefaultNavigationService>();
        serviceCollection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    }

    /// <summary>
    ///     注入 View Model
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddViewModels(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<MainViewModel>();

        // page view model
        serviceCollection.AddTransient<TVShowViewModel>();
    }

    /// <summary>
    ///     注入页面（Views）
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddViews(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<TVShowView>();
    }
}