using System;
using Avalonia.Controls;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class PlayerWindow : UrsaWindow
{
    public static Window? Window;

    public PlayerWindow()
    {
        InitializeComponent();
        Window = this;
    }

    protected override void OnClosed(EventArgs e)
    {
        VideoPlayer.Close();
        base.OnClosed(e);
        (App.VisualRoot as MainWindow)?.Show();
    }
}