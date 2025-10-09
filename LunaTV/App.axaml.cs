using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using LunaTV.Views;
using Ursa.Controls;

namespace LunaTV;

public class App : Application
{
    [NotNull] public static Visual? VisualRoot { get; internal set; }
    public static WindowNotificationManager? Notification { get; set; }
    public static WindowToastManager? Toast { get; set; }
    public static IStorageProvider? StorageProvider { get; internal set; }
    public static TopLevel TopLevel => TopLevel.GetTopLevel(VisualRoot)!;

    public static IServiceProvider Services => ServiceLocator.Host.Services;
    [NotNull] public static IClipboard? Clipboard { get; internal set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RequestedThemeVariant = ThemeVariant.Dark;
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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var window = ServiceLocator.GetRequiredService<MainWindow>();
            desktop.MainWindow = window;
            // window.DataContext = ServiceLocator.GetRequiredService<MainViewModel>();
            VisualRoot = window;
            Notification = new WindowNotificationManager(TopLevel);
            Toast = new WindowToastManager(TopLevel);

            StorageProvider = desktop.MainWindow.StorageProvider;
            Clipboard = desktop.MainWindow.Clipboard;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var view = ServiceLocator.GetRequiredService<MainView>();
            // view.DataContext = ServiceLocator.GetRequiredService<MainViewModel>();
            singleView.MainView = view;

            VisualRoot = view.Parent as MainWindow;
            StorageProvider = (view.Parent as MainWindow)?.StorageProvider;
            Clipboard = (view.Parent as MainWindow)?.Clipboard ?? throw new NullReferenceException("Clipboard is null");

            Notification = new WindowNotificationManager(TopLevel);
            Toast = new WindowToastManager(TopLevel);
        }

        // Notification.Position = NotificationPosition.BottomRight;
        // Toast.MaxItems = 2;
        base.OnFrameworkInitializationCompleted();
    }

    private void UIThreadOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.Exception);
        try
        {
            var win = new CrashWindow(e.Exception.ToString());
            win.Show();
        }
        finally
        {
            e.Handled = true;
        }
    }

    private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e);
        try
        {
            var win = new CrashWindow(e.ToString() ?? "Unhandled Exception");
            win.Show();
        }
        catch
        {
            // ignored
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}