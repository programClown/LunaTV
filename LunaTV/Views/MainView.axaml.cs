using System;
using Avalonia;
using Avalonia.Controls;
using LunaTV.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        try
        {
            DataContext = ServiceLocator.GetRequiredService<MainViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] MainViewModel creation FAILED: {ex}");
#if ANDROID
            global::Android.Util.Log.Error("LunaTV", $"MainViewModel CRASH: {ex}");
#endif
        }
    }
}