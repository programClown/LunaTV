using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LunaTV.Views;

public partial class InnovationPlazaView : UserControl
{
    public InnovationPlazaView()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new MpvPlayerWindow();
        window.Show();
    }
}