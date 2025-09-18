﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace HanumanInstitute.LibMpv.Avalonia;

public class SoftwareView : Control, IVideoView
{
    private WriteableBitmap? _renderTarget;

    // MpvContext property
    public static readonly DirectProperty<SoftwareView, MpvContext> MpvContextProperty = AvaloniaProperty.RegisterDirect<SoftwareView, MpvContext>(
        nameof(MpvContext), o => o.MpvContext, defaultBindingMode: BindingMode.OneWayToSource);
    public MpvContext MpvContext { get; } = new();

    public SoftwareView()
    {
        ClipToBounds = true;
    }

    protected override void OnInitialized()
    {
        MpvContext.StartSoftwareRendering(this.UpdateVideoView);
        base.OnInitialized();
    }

    public override void Render(DrawingContext context)
    {
        if (VisualRoot == null) { return; }

        var bitmapSize = GetPixelSize();
            
        if (_renderTarget == null || _renderTarget.PixelSize.Width != bitmapSize.Width || _renderTarget.PixelSize.Height != bitmapSize.Height)
            this._renderTarget = new WriteableBitmap(bitmapSize, new Vector(96.0, 96.0), PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var lockedBitmap = this._renderTarget.Lock())
        {
#if ANDROID
            var pix = "rgba";
#else
            var pix = "bgra";
#endif
            MpvContext.SoftwareRender(lockedBitmap.Size.Width, lockedBitmap.Size.Height, lockedBitmap.Address, pix);
        }
        context.DrawImage(this._renderTarget, new Rect(0, 0, _renderTarget.PixelSize.Width, _renderTarget.PixelSize.Height));
    }

    private PixelSize GetPixelSize()
    {
        var scaling = VisualRoot!.RenderScaling;
        //return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),Math.Max(1, (int)(Bounds.Height * scaling)));
        return new PixelSize((int)Bounds.Width, (int)Bounds.Height);
    }

    private void UpdateVideoView()
    {
        Dispatcher.UIThread.Post(this.InvalidateVisual, DispatcherPriority.Background);
    }

    protected virtual void Dispose(bool disposing)
    {
        // ReleaseUnmanagedResources();
        if (disposing)
        {
            MpvContext.Dispose();
            _renderTarget?.Dispose();
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    ~SoftwareView()
    {
        Dispose(false);
    }
}
