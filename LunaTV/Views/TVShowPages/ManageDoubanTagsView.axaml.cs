using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using LunaTV.ViewModels.TVShowPages;

namespace LunaTV.Views.TVShowPages;

public partial class ManageDoubanTagsView : UserControl
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private int _dragSourceIndex = -1;
    private int _currentHoverIndex = -1;
    private Border? _draggedBorder;
    private ListBoxItem? _highlightedItem;
    private const double DragThreshold = 5;

    /// <summary>
    /// Brush used to render the drop-target insert indicator (colored top border).
    /// </summary>
    private static readonly IBrush DragInsertBrush =
        new SolidColorBrush(Color.FromArgb(0x80, 0x60, 0x94, 0xEA));

    public ManageDoubanTagsView()
    {
        InitializeComponent();
    }

    private void NewTagInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ManageDoubanTagsViewModel vm && sender is TextBox tb)
        {
            // Text binding uses default UpdateSourceTrigger=LostFocus, so
            // the VM property is stale when Enter fires. Push the current
            // text to the VM before executing the add command.
            vm.NewTagInput = tb.Text;

            vm.AddTagCommand.Execute(null);
        }
    }

    private static ListBoxItem? FindListBoxItem(Control? element)
    {
        while (element is not null)
        {
            if (element is ListBoxItem item) return item;
            element = element.GetVisualParent() as Control;
        }
        return null;
    }

    private void TagItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        var props = e.GetCurrentPoint(border).Properties;
        if (!props.IsLeftButtonPressed) return;

        var container = FindListBoxItem(border);
        if (container is null) return;

        _dragStartPoint = e.GetPosition(container);
        _dragSourceIndex = TagList.IndexFromContainer(container);
        _currentHoverIndex = _dragSourceIndex;
        _draggedBorder = border;
        _isDragging = false;

        e.Pointer.Capture(border);
        e.Handled = true;
    }

    private void TagItem_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedBorder is null || _dragSourceIndex < 0) return;
        if (e.Pointer.Captured != _draggedBorder) return;

        var currentPos = e.GetPosition(_draggedBorder);
        var dx = Math.Abs(currentPos.X - _dragStartPoint.X);
        var dy = Math.Abs(currentPos.Y - _dragStartPoint.Y);

        if (!_isDragging && (dx > DragThreshold || dy > DragThreshold))
        {
            _isDragging = true;
            ApplyDragVisual(true);
        }

        if (!_isDragging) return;
        if (DataContext is not ManageDoubanTagsViewModel vm) return;

        // Determine which item the pointer is currently over.
        // We do NOT call vm.MoveTag here.  ObservableCollection.Move() tears down
        // and rebuilds ListBox containers, which would destroy our pointer capture
        // and invalidate _draggedBorder.  Instead we track the intended drop index
        // and highlight it visually; the actual collection change happens once,
        // on PointerReleased.
        var posInList = e.GetPosition(TagList);
        var hoverIndex = -1;

        for (var i = 0; i < vm.Tags.Count; i++)
        {
            var container = TagList.ContainerFromIndex(i) as Control;
            if (container is null) continue;
            if (container.Bounds.Contains(posInList))
            {
                hoverIndex = i;
                break;
            }
        }

        // When the pointer lands in a gap between items, retain the last-known
        // hover target so the insert indicator does not flicker.
        if (hoverIndex < 0)
            hoverIndex = _currentHoverIndex;

        if (hoverIndex != _currentHoverIndex)
        {
            HighlightTarget(hoverIndex);
            _currentHoverIndex = hoverIndex;
        }
    }

    private void HighlightTarget(int index)
    {
        RemoveHighlight();

        // Do not show a self-target highlight when hovering over the source.
        if (index < 0 || index == _dragSourceIndex) return;

        var container = TagList.ContainerFromIndex(index);
        if (container is ListBoxItem lbi)
        {
            lbi.BorderBrush = DragInsertBrush;
            lbi.BorderThickness = new Thickness(0, 2, 0, 0);
            _highlightedItem = lbi;
        }
    }

    private void RemoveHighlight()
    {
        if (_highlightedItem is not null)
        {
            _highlightedItem.ClearValue(ListBoxItem.BorderBrushProperty);
            _highlightedItem.ClearValue(ListBoxItem.BorderThicknessProperty);
            _highlightedItem = null;
        }
    }

    private void ApplyDragVisual(bool dragging)
    {
        if (_draggedBorder is null) return;
        _draggedBorder.Opacity = dragging ? 0.4 : 1.0;
        _draggedBorder.Cursor = new Cursor(dragging ? StandardCursorType.SizeAll : StandardCursorType.Hand);
        if (dragging)
        {
            _draggedBorder.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            _draggedBorder.RenderTransform = new ScaleTransform(1.05, 1.05);
        }
        else
        {
            _draggedBorder.RenderTransformOrigin = RelativePoint.TopLeft;
            _draggedBorder.RenderTransform = null;
        }
    }

    /// <summary>
    /// Releases pointer capture, restores all visual state, and optionally
    /// performs a single MoveTag to commit the reorder.
    /// </summary>
    private void EndDrag(IPointer pointer, bool shouldMove)
    {
        if (shouldMove
            && _isDragging
            && _currentHoverIndex >= 0
            && _currentHoverIndex != _dragSourceIndex)
        {
            if (DataContext is ManageDoubanTagsViewModel vm)
            {
                vm.MoveTag(_dragSourceIndex, _currentHoverIndex);
            }
        }

        RemoveHighlight();
        ApplyDragVisual(false);
        pointer.Capture(null);
        _isDragging = false;
        _draggedBorder = null;
        _dragSourceIndex = -1;
        _currentHoverIndex = -1;
    }

    private void TagItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndDrag(e.Pointer, shouldMove: true);
    }

    private void TagItem_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // If another element steals capture (system gesture, window deactivation,
        // etc.), clean up without committing a move -- the user did not drop
        // intentionally.
        EndDrag(e.Pointer, shouldMove: false);
    }
}
