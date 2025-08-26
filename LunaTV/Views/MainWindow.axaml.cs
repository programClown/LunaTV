using Avalonia;
using Avalonia.Controls;
using System.Runtime.InteropServices;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();

        ApplyPlatformSpecificMargin();
    }
    
    private void ApplyPlatformSpecificMargin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            LeftTitlebar.Margin = new Thickness(60,0,0,0);
        }
    }
}