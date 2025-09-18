﻿using System.Runtime.CompilerServices;
using HanumanInstitute.LibMpv.Core;

namespace HanumanInstitute.LibMpv;

public unsafe partial class MpvContextBase : IDisposable
{
    private bool _disposed;
    private readonly IEventLoop _eventLoop;
    protected MpvHandle* Ctx => _ctx == null ? InitCtx() :
        !_disposed ? _ctx : throw new ObjectDisposedException(nameof(MpvContextBase));
    private MpvHandle* _ctx;
    private MpvHandle* InitCtx()
    {
        _ctx = MpvApi.Create();
        if (_ctx == null)
        {
            throw new MpvException("Unable to create mpv_context. Currently, this can happen in the following situations - out of memory or LC_NUMERIC is not set to \"C\"");
        }
        return _ctx;
    }
    
    public MpvContextBase() : this(MpvEventLoop.Default)
    {
    }

    public MpvContextBase(MpvEventLoop mpvEventLoop)
    {
#if ANDROID
        InitAndroid.InitJvm();
#endif

        Initialize();
        InitEventHandlers();

        _eventLoop = mpvEventLoop switch
        {
            MpvEventLoop.Default => new MpvSimpleEventLoop(Ctx, this.HandleEvent),
            MpvEventLoop.Thread => new MpvThreadEventLoop(Ctx, this.HandleEvent),
            _ => new MpvWeakEventLoop(Ctx, this.HandleEvent)
        };
        _eventLoop.Start();
    }

    ~MpvContextBase()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            StopRendering();
            _eventLoop.Stop();
            if (_eventLoop is IDisposable disposable)
            {
                disposable.Dispose();
            }
            MpvApi.TerminateDestroy(Ctx);
            _disposed = true;
        }
    }
}
