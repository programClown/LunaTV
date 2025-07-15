using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LunaTV;

/// <summary>
/// </summary>
public static class ServiceLocator
{
    private static IHost? _host;

    /// <summary>
    ///     Host 实例，全局唯一
    /// </summary>
    public static IHost Host
    {
        get => _host!;
        set
        {
            if (_host is not null) return;
            _host = value;
        }
    }

    /// <summary>
    ///     获取服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public static T GetRequiredService<T>() where T : class
    {
        return (T)Host.Services.GetRequiredService(typeof(T));
    }

    /// <summary>
    ///     获取服务
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例</returns>
    public static object GetRequiredService(Type serviceType)
    {
        return Host.Services.GetRequiredService(serviceType);
    }
}