using System;

namespace LunaTV.Services;

/// <summary>
///     导航服务
/// </summary>
public interface INavigationService
{
    /// <summary>
    ///     导航到指定页面
    /// </summary>
    /// <param name="vmType">页面视图模型类型</param>
    void NavigateTo(Type vmType);
}