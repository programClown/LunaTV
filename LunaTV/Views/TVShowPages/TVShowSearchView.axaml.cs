using Avalonia;
using Avalonia.Controls;
using LunaTV.ViewModels.TVShowPages;

namespace LunaTV.Views.TVShowPages;

public partial class TVShowSearchView : UserControl
{
    public TVShowSearchView()
    {
        InitializeComponent();
        SearchResultScrollViewer.SizeChanged += OnSearchResultSizeChanged;
    }

    private void OnSearchResultSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is not TVShowSearchViewModel vm) return;

        vm.UpdatePageSize(SearchResultScrollViewer.Bounds.Width, SearchResultScrollViewer.Bounds.Height);
    }
}