using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class CrashWindow : UrsaWindow
{
    public CrashWindow(string exception)
    {
        InitializeComponent();

        Info.Text = exception;
        Copy.Click += async (_, _) =>
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            await clipboard!.SetTextAsync(exception);
        };
        Continue.Click += (_, _) => { Close(); };
        Exit.Click += (_, _) => { Environment.Exit(0); };
        Topmost = true;
        Show();
        Activate();
    }

    public CrashWindow()
    {
    }

    public sealed override void Show()
    {
        base.Show();
    }
}