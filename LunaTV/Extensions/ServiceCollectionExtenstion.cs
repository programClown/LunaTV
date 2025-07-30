using System;
using CommunityToolkit.Mvvm.Messaging;
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
        serviceCollection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
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
}