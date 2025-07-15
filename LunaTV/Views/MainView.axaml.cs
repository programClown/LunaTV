using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels;

namespace LunaTV.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        NaviView.SelectionChanged += NaviViewOnSelectionChanged;
        NaviView.SelectedItem = NaviView.MenuItems.ElementAt(0);
    }

    private void NaviViewOnSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.IsSettingsSelected)
        {
        }
        else if (e.SelectedItem is NavigationViewItem nvi)
        {
        }
    }
}