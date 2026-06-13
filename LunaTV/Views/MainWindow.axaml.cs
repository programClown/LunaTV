using Avalonia;
using Avalonia.Controls;
using System;
using System.Runtime.InteropServices;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();

        ApplyPlatformSpecificMargin();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
#if !ANDROID
        if (!e.Cancel) DoubanVerifyWindow.CloseAll();
#endif
    }

    protected override void OnClosed(EventArgs e)
    {
        App.BeginShutdown();
#if !ANDROID
        DoubanVerifyWindow.CloseAll();
#endif
        base.OnClosed(e);
    }

    private void ApplyPlatformSpecificMargin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            LeftTitlebar.Margin = new Thickness(60, 0, 0, 0);
        }
    }
}