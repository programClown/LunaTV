using Avalonia;
using Avalonia.Controls;
using LunaTV.ViewModels.TVShowPages;

namespace LunaTV.Views.TVShowPages;

public partial class TVShowHomeView : UserControl
{
    public TVShowHomeView()
    {
        InitializeComponent();
        MovieCardScrollViewer.SizeChanged += OnMovieCardScrollViewerSizeChanged;
    }

    private void OnMovieCardScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is not TVShowHomeViewModel vm) return;

        vm.UpdatePageSize(MovieCardScrollViewer.Bounds.Width, MovieCardScrollViewer.Bounds.Height);
    }
}