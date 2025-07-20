using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using Semi.Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels;

public partial class SettingsViewModel : PageViewModelBase
{
    public override string Title => "设置";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("SettingsIcon", out var value) ? (IconSource)value : null;
    
    public SettingsViewModel()
    {
    }

    public string CurrentVersion => typeof(App).Assembly.GetName().Version?.ToString();
    public string CurrentAvaloniaVersion => typeof(Application).Assembly.GetName().Version?.ToString();

    public List<string> AppThemes => ["Auto", "Light", "Dark", "Aquatic", "Desert", "Dusk", "NightSky"];
    
    [ObservableProperty]
    private string? _currentAppTheme = "Auto";

    public FlowDirection[] AppFlowDirections { get; } =
        new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft };
    
    [ObservableProperty]
    private FlowDirection _currentFlowDirection;
    
    partial void OnCurrentAppThemeChanged(string? value)
    {
        var app = App.Current;
        if (app is null) return;
        ThemeVariant theme = value switch
        {
            "Auto" => app.ActualThemeVariant== ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "Aquatic" => SemiTheme.Aquatic,
            "Desert" => SemiTheme.Desert,
            "Dusk" => SemiTheme.Dusk,
            "NightSky" => SemiTheme.NightSky,
            _ => app.ActualThemeVariant== ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark
        };
        app.RequestedThemeVariant = theme;
        
        App.Notification?.Show(
            new Notification("主题已更新", $"当前主题是{value}"),
            type: NotificationType.Success,
            classes: ["Light"]);
        
    }
    
    partial void OnCurrentFlowDirectionChanged(FlowDirection value)
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime cdl)
        {
            if (cdl.MainWindow.FlowDirection == value)
                return;
            cdl.MainWindow.FlowDirection = value;
        }
    }
}