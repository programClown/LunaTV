using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using LunaTV.Views;

namespace LunaTV.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private PageViewModelBase? _currentPage;

    [ObservableProperty] private List<PageViewModelBase> _footerPages = new();

    [ObservableProperty] private List<PageViewModelBase> _pages = new();

    [ObservableProperty] private object? _selectedCategory;

    public MainViewModel()
    {
        NavigationFactory = new NavigationFactory(this);
    }

    public NavigationFactory NavigationFactory { get; }

    public double PaneWidth => 200;

    public void Loaded()
    {
        // Set only if null, since this may be called again when content dialogs open
        CurrentPage ??= Pages.FirstOrDefault();
        SelectedCategory ??= Pages.FirstOrDefault();
    }
}

public class NavigationFactory : INavigationPageFactory
{
    public NavigationFactory(MainViewModel owner)
    {
        Owner = owner;
    }

    public MainViewModel Owner { get; }

    public Control GetPage(Type srcType)
    {
        return null;
    }

    public Control GetPageFromObject(object target)
    {
        if (target is PageViewModelBase)
        {
            var viewTypeName = target.GetType().FullName!.Replace("ViewModel", "View");
            var viewType = Type.GetType(viewTypeName);
            var notFound = new TextBlock { Text = "View not found for " + viewTypeName };
            if (viewType is null || ServiceLocator.GetRequiredService(viewType) is not Control view) return notFound;

            view.DataContext = target;
            return view;
        }

        if (target is string)
            if (target.Equals("设置"))
                return ServiceLocator.GetRequiredService<SettingsView>();

        return new TextBlock { Text = "啥也没找到啊" };
        ;
    }
}