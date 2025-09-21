using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HanumanInstitute.LibMpv;
using LunaTV.Extensions;
using LunaTV.ViewModels;
using LunaTV.Views.Media;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class MpvPlayerWindow : UrsaWindow
{
    /// <summary>
    /// Gets the name of the seek bar.
    /// </summary>
    public const string SeekBarPartName = "PART_SeekBar";

    /// <summary>
    /// Gets the seek bar slider control.
    /// </summary>
    public Slider? SeekBarPart { get; private set; }

    /// <summary>
    /// Gets the name of the track within the seek bar.
    /// </summary>
    public const string SeekBarTrackPartName = "PART_Track";

    /// <summary>
    /// Gets the track within the seek bar.
    /// </summary>
    public Track? SeekBarTrackPart { get; private set; }

    /// <summary>
    /// The name of the seek bar decrease button.
    /// </summary>
    public const string SeekBarDecreaseName = "PART_DecreaseButton";

    /// <summary>
    /// Gets the seek bar decrease button. 
    /// </summary>
    public RepeatButton? SeekBarDecreasePart { get; private set; }

    /// <summary>
    /// The name of the seek bar increase button.
    /// </summary>
    public const string SeekBarIncreaseName = "PART_IncreaseButton";

    /// <summary>
    /// Gets the seek bar increase button.
    /// </summary>
    public RepeatButton? SeekBarIncreasePart { get; private set; }

    /// <summary>
    /// Gets the thumb within the seek bar. 
    /// </summary>
    public Thumb? SeekBarThumbPart => SeekBarTrackPart?.Thumb;

    private readonly MpvPlayerWindowModel _viewModel;

    public MpvPlayerWindow()
    {
        InitializeComponent();

        _viewModel = new MpvPlayerWindowModel();
        DataContext = _viewModel;
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
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        (DataContext as MpvPlayerWindowModel)?.OnWindowLoaded();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _viewModel?.StopCommand.Execute(null);
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