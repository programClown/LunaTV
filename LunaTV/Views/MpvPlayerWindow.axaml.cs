using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using LunaTV.Extensions;
using LunaTV.ViewModels;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class MpvPlayerWindow : UrsaWindow
{
    /// <summary>
    /// Gets the name of the seek bar.
    /// </summary>
    private const string SeekBarPartName = "PART_SeekBar";

    /// <summary>
    /// Gets the seek bar slider control.
    /// </summary>
    private Slider? SeekBarPart { get; set; }

    /// <summary>
    /// Gets the name of the track within the seek bar.
    /// </summary>
    private const string SeekBarTrackPartName = "PART_Track";

    /// <summary>
    /// Gets the track within the seek bar.
    /// </summary>
    private Track? SeekBarTrackPart { get; set; }

    /// <summary>
    /// The name of the seek bar decrease button.
    /// </summary>
    private const string SeekBarDecreaseName = "PART_DecreaseButton";

    /// <summary>
    /// Gets the seek bar decrease button. 
    /// </summary>
    private RepeatButton? SeekBarDecreasePart { get; set; }

    /// <summary>
    /// The name of the seek bar increase button.
    /// </summary>
    private const string SeekBarIncreaseName = "PART_IncreaseButton";

    /// <summary>
    /// Gets the seek bar increase button.
    /// </summary>
    private RepeatButton? SeekBarIncreasePart { get; set; }

    /// <summary>
    /// Gets the thumb within the seek bar. 
    /// </summary>
    private Thumb? SeekBarThumbPart => SeekBarTrackPart?.Thumb;

    private readonly MpvPlayerWindowModel _viewModel;

    public MpvPlayerWindow()
    {
        InitializeComponent();

        _viewModel = new MpvPlayerWindowModel();
        DataContext = _viewModel;

        _viewModel.Notification = new WindowNotificationManager(this);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        SeekBarPart = this.FindControl<Slider>(SeekBarPartName);

        // Thumb doesn't yet exist.
        if (SeekBarPart != null)
            SeekBarPart.TemplateApplied += (_, t) =>
            {
                SeekBarTrackPart = t.NameScope.FindOrThrow<Track>(SeekBarTrackPartName);
                SeekBarIncreasePart = t.NameScope.FindOrThrow<RepeatButton>(SeekBarIncreaseName);
                SeekBarDecreasePart = t.NameScope.FindOrThrow<RepeatButton>(SeekBarDecreaseName);

                SeekBarIncreasePart.AddHandler(RepeatButton.PointerPressedEvent, SeekBarPointerPressed,
                    RoutingStrategies.Tunnel);
                SeekBarDecreasePart.AddHandler(RepeatButton.PointerPressedEvent, SeekBarPointerPressed,
                    RoutingStrategies.Tunnel);
                SeekBarIncreasePart.AddHandler(RepeatButton.PointerReleasedEvent, SeekBarPointerReleased,
                    RoutingStrategies.Tunnel);
                SeekBarDecreasePart.AddHandler(RepeatButton.PointerReleasedEvent, SeekBarPointerReleased,
                    RoutingStrategies.Tunnel);

                SeekBarThumbPart!.DragStarted += (_, _) => _viewModel.IsSeekBarPressed = true;
                SeekBarThumbPart!.DragCompleted += (_, _) => _viewModel.IsSeekBarPressed = false;
            };

        _viewModel?.OnWindowLoaded();
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _viewModel?.Stop();
        base.OnClosing(e);
    }

    private void SeekBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _viewModel.IsSeekBarPressed = true;
    }

    private void SeekBarPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _viewModel.IsSeekBarPressed = false;
    }
}