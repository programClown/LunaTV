using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class PlayerWindow : UrsaWindow
{
    public PlayerWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        VideoPlayer.Close();
        base.OnClosed(e);
        (App.VisualRoot as MainWindow)?.Show();
    }
}