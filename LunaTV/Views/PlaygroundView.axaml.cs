using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using LunaTV.ViewModels;

namespace LunaTV.Views;

public partial class PlaygroundView : UserControl
{
    public PlaygroundView()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var playgroundViewModel = DataContext as PlaygroundViewModel;
        if (playgroundViewModel is not null)
        {
            Editor.DataContext = playgroundViewModel.PlaygroundNodeViewModel;
        }
    }
}