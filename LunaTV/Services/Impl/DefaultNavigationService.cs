using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LunaTV.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.Services.Impl;

/// <summary>
///     当前页面变更消息
/// </summary>
public class CurrentPageChangedMessage(ViewModelBase? currentPage) : ValueChangedMessage<ViewModelBase?>(currentPage);

/// <summary>
///     导航服务的默认实现
/// </summary>
public class DefaultNavigationService(IMessenger messenger) : INavigationService
{
    /// <summary>
    ///     当前页面对应的 view model
    /// </summary>
    private ViewModelBase? CurrentPage { get; set; }

    /// <inheritdoc />
    public void NavigateTo(Type vmType)
    {
        if (!typeof(ViewModelBase).IsAssignableFrom(vmType))
        {
            Debug.WriteLine($"页面导航出错：{vmType} 不是 {nameof(ViewModelBase)} 类型");
            return;
        }

        CurrentPage = ServiceLocator.Host.Services.GetRequiredService(vmType) as ViewModelBase;
        messenger.Send(new CurrentPageChangedMessage(CurrentPage));
    }
}