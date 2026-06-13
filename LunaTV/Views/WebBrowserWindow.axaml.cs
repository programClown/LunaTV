using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LunaTV.Views;

public partial class WebBrowserWindow : Window
{
    public WebBrowserWindow()
    {
        InitializeComponent();
        BrowserWebView.NewWindowRequested += OnNewWindowRequested;
    }

    public WebBrowserWindow(string url) : this()
    {
        NavigateTo(url);
    }

    public WebBrowserWindow(Uri source) : this()
    {
        NavigateTo(source);
    }

    private void NavigateTo(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        NavigateTo(uri);
    }

    private void NavigateTo(Uri source)
    {
        BrowserWebView.Source = source;
        var title = string.IsNullOrWhiteSpace(source.Host) ? source.ToString() : source.Host;
        TitleTextBlock.Text = title;
        Title = title;
    }

    private void OnNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        BrowserWebView.Source = e.Request;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        BrowserWebView.NewWindowRequested -= OnNewWindowRequested;
    }
}
