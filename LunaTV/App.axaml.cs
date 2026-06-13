using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using LunaTV.Constants;
using LunaTV.ViewModels;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;
using N_m3u8DL_RE.Common.Util;
using Ursa.Controls;

namespace LunaTV;

public class App : Application
{
    [NotNull] public static Visual? VisualRoot { get; internal set; }
    public static WindowNotificationManager? Notification { get; set; }
    public static WindowToastManager? Toast { get; set; }
    public static bool IsShuttingDown { get; private set; }
    public static IStorageProvider? StorageProvider { get; internal set; }
    public static TopLevel? TopLevel => VisualRoot != null ? TopLevel.GetTopLevel(VisualRoot) : null;

    public static IServiceProvider Services => ServiceLocator.Host.Services;
    [NotNull] public static IClipboard? Clipboard { get; internal set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RequestedThemeVariant = ThemeVariant.Dark;

#if ANDROID
        // Load Android-specific styles for mobile UI adaptation
        var androidStyles = new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://LunaTV"))
        {
            Source = new Uri("/Styles/AndroidStyle.axaml", UriKind.Relative)
        };
        Styles.Add(androidStyles);
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!Debugger.IsAttached)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Dispatcher.UIThread.UnhandledException += UIThreadOnUnhandledException;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) => IsShuttingDown = true;
            GlobalDefine.FFmpegPath = GlobalUtil.FindExecutable("ffmpeg");
            var window = ServiceLocator.GetRequiredService<MainWindow>();
            desktop.MainWindow = window;
            VisualRoot = window;
            Notification = new WindowNotificationManager(TopLevel);
            Toast = new WindowToastManager(TopLevel);

            StorageProvider = desktop.MainWindow.StorageProvider;
            Clipboard = desktop.MainWindow.Clipboard;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
#if ANDROID
            // Extract ffmpeg binary from assets on first run
            InitAndroidFfmpeg();
#endif
            var view = ServiceLocator.GetRequiredService<MainView>();
            singleView.MainView = view;

            VisualRoot = view;

            // On single-view platforms (Android, iOS) there is no MainWindow.
            // TopLevel is only available once the view is attached to the visual tree,
            // so we defer StorageProvider / Clipboard / Notification initialization.
            view.AttachedToVisualTree += (_, _) =>
            {
                var topLevel = TopLevel.GetTopLevel(view);
                if (topLevel is null) return;

                StorageProvider = topLevel.StorageProvider;
                Clipboard = topLevel.Clipboard;
                Notification = new WindowNotificationManager(topLevel);
                Toast = new WindowToastManager(topLevel);
            };
        }

        // Notification.Position = NotificationPosition.BottomRight;
        // Toast.MaxItems = 2;
        base.OnFrameworkInitializationCompleted();
    }

    public static void BeginShutdown()
    {
        IsShuttingDown = true;
    }

    private void UIThreadOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.Exception);
        if (IsShuttingDown)
        {
            e.Handled = true;
            return;
        }

        try
        {
#if !ANDROID
            var win = new CrashWindow(e.Exception.ToString());
            win.Show();
#else
            System.Diagnostics.Trace.WriteLine($"[LunaTV Crash] {e.Exception}");
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var mainVm = Services.GetService<MainViewModel>();
                        if (mainVm != null)
                        {
                            mainVm.PageContent = new AndroidCrashView(e.Exception.ToString());
                        }
                    }
                    catch { }
                });
            }
            catch { }
#endif
        }
        finally
        {
            e.Handled = true;
        }
    }

    private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e);
        if (IsShuttingDown) return;

        try
        {
#if !ANDROID
            var win = new CrashWindow(e.ToString() ?? "Unhandled Exception");
            win.Show();
#else
            var exMsg = e.ExceptionObject?.ToString() ?? "Unknown error";
            System.Diagnostics.Trace.WriteLine($"[LunaTV Crash] {exMsg}");
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var mainVm = Services.GetService<MainViewModel>();
                        if (mainVm != null)
                        {
                            mainVm.PageContent = new AndroidCrashView(exMsg);
                        }
                    }
                    catch { }
                });
            }
            catch { }
#endif
        }
        catch
        {
            // ignored
        }
    }

#if ANDROID
    private static void InitAndroidFfmpeg()
    {
        try
        {
            // Android extracts native libraries to the app's nativeLibraryDir
            var context = global::Android.App.Application.Context;
            var nativeLibDir = context.ApplicationInfo?.NativeLibraryDir;
            if (string.IsNullOrEmpty(nativeLibDir)) return;

            var ffmpegInLib = System.IO.Path.Combine(nativeLibDir, "libffmpeg.so");
            if (System.IO.File.Exists(ffmpegInLib))
            {
                // Copy to a non-.so name so Process.Start can execute it
                var ffmpegDir = System.IO.Path.Combine(GlobalDefine.DataPath, "bin");
                var ffmpegPath = System.IO.Path.Combine(ffmpegDir, "ffmpeg");

                if (!System.IO.File.Exists(ffmpegPath))
                {
                    if (!System.IO.Directory.Exists(ffmpegDir))
                        System.IO.Directory.CreateDirectory(ffmpegDir);
                    System.IO.File.Copy(ffmpegInLib, ffmpegPath, true);
                }

                // Ensure executable
                try { Java.Lang.Runtime.GetRuntime().Exec($"chmod 755 {ffmpegPath}")?.WaitFor(); } catch { }
                GlobalDefine.FFmpegPath = ffmpegPath;
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] InitAndroidFfmpeg failed: {ex.Message}");
        }
    }
#endif
}