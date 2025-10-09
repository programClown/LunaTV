using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using LunaTV.Animations;
using LunaTV.Services;
using LunaTV.ViewModels;
using LunaTV.ViewModels.Base;
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

        var vm = App.Services.GetRequiredService<MainViewModel>();
        DataContext = vm;

        FrameView.NavigationPageFactory = vm.NavigationFactory;
        NavigationService.Instance.SetFrame(FrameView);

        NaviView.ItemInvoked += OnNaviViewItemInvoked;

        Dispatcher.UIThread.Invoke((() =>
        {
            vm.Loaded();
            FrameView.NavigateFromObject(NaviView.MenuItemsSource.ElementAt(0), new FrameNavigationOptions
            {
                TransitionInfoOverride = new BetterEntranceNavigationTransition()
            });
        }));
    }


    private void OnNaviViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem nvi)
        {
            // Skip if this is the currently selected item
            if (nvi.IsSelected) return;

            if (nvi.Tag is null) throw new InvalidOperationException("NavigationViewItem Tag is null");

            NavigationService.Instance.NavigateFromContext(nvi.Tag, new BetterEntranceNavigationTransition());
        }
    }

    private void SetNVIIcon(NavigationViewItem item, bool selected)
    {
        // Technically, yes you could set up binding and converters and whatnot to let the icon change
        // between filled and unfilled based on selection, but this is so much simpler 

        if (item == null)
            return;

        var t = item.Tag;

        if (t is TVShowViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "VideoIconFilled" : "VideoIcon", out var value)
                ? (IconSource)value
                : null;
        }
        else if (item is SettingsViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "SettingsIconFilled" : "SettingsIcon", out var value)
                ? (IconSource)value
                : null;
        }
    }
}