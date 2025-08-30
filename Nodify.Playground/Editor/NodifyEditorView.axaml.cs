namespace Nodify.Playground;

public partial class NodifyEditorView : UserControl
{
    public NodifyEditorView()
    {
        InitializeComponent();
    }

    public NodifyEditor EditorInstance => Editor;

    private void Minimap_Zoom(object sender, RoutedEventArgs e)
    {
        if (e is ZoomEventArgs zoomEventArgs) EditorInstance.ZoomAtPosition(zoomEventArgs.Zoom, zoomEventArgs.Location);
    }
}