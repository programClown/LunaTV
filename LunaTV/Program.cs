using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Media;
using LunaTV.Constants;
using LunaTV.Extensions;
using LunaTV.Models;
using LunaTV.Services;
using Microsoft.Extensions.Hosting;
using System;

using System.Text;

namespace LunaTV;

internal sealed class Program
{
#if !ANDROID
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // windows下输出中文
        if (OperatingSystem.IsWindows())
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
#endif

    /// <summary>
    ///     Shared host/DI setup and font configuration for both desktop and Android.
    ///     Call this from your AvaloniaMainActivity.CustomizeAppBuilder before
    ///     the activity returns.  Does NOT include desktop-only platform options.
    /// </summary>
    public static AppBuilder ConfigureSharedAppBuilder(AppBuilder builder)
    {
        AppJsonConfig appJsonConfig;
        try
        {
            appJsonConfig = AppJsonConfigService.ReadJson<AppJsonConfig>(GlobalDefine.AppJsonPath) ?? new AppJsonConfig();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] Config read failed: {ex.Message}");
            appJsonConfig = new AppJsonConfig();
        }
        appJsonConfig.Player ??= new Player();
        appJsonConfig.StoragePaths ??= new StoragePathsConfig();

        try
        {
            GlobalDefine.ApplyStoragePaths(appJsonConfig.StoragePaths);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] ApplyStoragePaths failed: {ex.Message}");
        }

        IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddViewModels();
                services.AddServices();
                services.AddViews();
                services.AddDb();
            }).Build();
        ServiceLocator.Host = host;

        return builder
            .With(new FontManagerOptions
            {
                FontFallbacks = new[]
                {
                    new FontFallback
                    {
                        FontFamily = new FontFamily(GetFontFamily())
                    }
                }
            })
            .LogToTrace();
    }

#if !ANDROID
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return ConfigureSharedAppBuilder(AppBuilder.Configure<App>())
#pragma warning disable CA1416
            .UseManagedSystemDialogs()
#pragma warning restore CA1416
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                RenderingMode = new[] { X11RenderingMode.Glx, X11RenderingMode.Egl }
            })
            .With(new AvaloniaNativePlatformOptions
            {
                RenderingMode =
                [
                    // put OpenGL first, to have higher priority over Metal
                    AvaloniaNativeRenderingMode.OpenGl,
                    AvaloniaNativeRenderingMode.Metal,
                    AvaloniaNativeRenderingMode.Software
                ]
            })
            .With(new Win32PlatformOptions());
    }
#endif

    private static string GetFontFamily()
    {
        if (OperatingSystem.IsWindows())
        {
            // windows下使用微软雅黑
            return "微软雅黑";
        }
        if (OperatingSystem.IsMacOS())
        {
            // macos下使用pingfang sc
            return "PingFang SC";
        }
        if (OperatingSystem.IsAndroid())
        {
            // Android uses Noto Sans CJK by default
            return "Noto Sans CJK SC";
        }

        return "";
    }
}