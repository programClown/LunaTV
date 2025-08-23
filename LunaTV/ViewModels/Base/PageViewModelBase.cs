using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;

namespace LunaTV.ViewModels.Base;

/// <summary>
///     An abstract class for enabling page navigation.
/// </summary>
public abstract class PageViewModelBase : ViewModelBase, IDisposable
{
    private bool _disposed;


    public abstract string Title { get; }
    public abstract IconSource IconSource { set; get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
    }
}