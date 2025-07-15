using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LunaTV.ViewModels;

/// <summary>
///     带消息订阅功能的 ViewModel 基类
/// </summary>
public class RecipientViewModelBase : ObservableRecipient, IDisposable
{
    /// <summary>
    ///     资源是否已释放
    /// </summary>
    private bool _disposed;

    protected RecipientViewModelBase()
    {
        // 注册事件
        IsActive = true;
    }

    #region 释放资源

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing) IsActive = false;

        _disposed = true;
    }

    #endregion
}