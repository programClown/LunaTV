using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HanumanInstitute.LibMpv;
using LunaTV.ViewModels;
using LunaTV.Views.Media;
using Ursa.Controls;

namespace LunaTV.Views;

public partial class MpvPlayerWindow : UrsaWindow
{
    public MpvPlayerWindow()
    {
        InitializeComponent();

        DataContext = new MpvPlayerWindowModel();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        (DataContext as MpvPlayerWindowModel)?.OnWindowLoaded();
    }
}