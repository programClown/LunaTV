﻿using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;

namespace HanumanInstitute.LibMpv.Avalonia;

public class MpvView : Control
{
    private IVideoView? _view;
    public event EventHandler? ViewInitialized;

    // MpvContext property
    public static readonly DirectProperty<MpvView, MpvContext?> MpvContextProperty = AvaloniaProperty.RegisterDirect<MpvView, MpvContext?>(
        nameof(MpvContext), o => o.MpvContext, defaultBindingMode: BindingMode.OneWayToSource);
    public MpvContext? MpvContext => _view?.MpvContext;

    /// <summary>
    /// Defines the Renderer property.
    /// </summary>
    public static readonly DirectProperty<MpvView, VideoRenderer> RendererProperty =
        AvaloniaProperty.RegisterDirect<MpvView, VideoRenderer>(
            nameof(Renderer), o => o.Renderer, (o, v) => o.Renderer = v);
    private VideoRenderer _renderer = Avalonia.VideoRenderer.Auto;
    /// <summary>
    /// Gets or sets the video renderer.
    /// </summary>
    public VideoRenderer Renderer
    {
        get => _renderer;
        set
        {
            if (SetAndRaise(RendererProperty, ref _renderer, value))
            {
                InitRenderer();
            }
        }
    }

    protected override void OnInitialized()
    {
        InitRenderer();
    }

    public void InitRenderer()
    {
        StopRenderer();

        var oldContext = MpvContext;
        _view = Renderer switch
        {
            VideoRenderer.Software => new SoftwareView(),
            VideoRenderer.OpenGl => new OpenGlView(),
            VideoRenderer.Native => new NativeView(),
            _ => SelectAuto()
        };

        this.VisualChildren.Add((Visual)_view);
        RaisePropertyChanged(MpvContextProperty, oldContext, MpvContext);
    }

    private IVideoView SelectAuto()
    {
#if ANDROID
        return new NativeView();
#endif
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            new NativeView() :
            new OpenGlView();
    }

    private void StopRenderer()
    {
        if (_view == null) { return; }

        var oldContext = MpvContext;
        this.VisualChildren.Remove((Visual)_view);
        _view?.Dispose();
        _view = null;
        this.RaisePropertyChanged(MpvContextProperty, oldContext, null);
    }
}
