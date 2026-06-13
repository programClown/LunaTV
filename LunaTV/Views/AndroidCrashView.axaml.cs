#if ANDROID
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using LunaTV.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.Views;

public partial class AndroidCrashView : UserControl
{
    private readonly UserControl? _previousContent;
    private readonly string _errorMessage;

    public AndroidCrashView()
    {
        InitializeComponent();
    }

    public AndroidCrashView(string errorMessage) : this()
    {
        _errorMessage = errorMessage;
        var mainVm = App.Services.GetService<MainViewModel>();
        _previousContent = mainVm?.PageContent;

        ErrorText.Text = errorMessage;
        CopyButton.Click += OnCopyClick;
        ContinueButton.Click += OnContinueClick;
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(_errorMessage);
            }
        }
        catch { }
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        var mainVm = App.Services.GetService<MainViewModel>();
        if (mainVm != null && _previousContent != null)
        {
            mainVm.PageContent = _previousContent;
        }
    }
}
#endif
