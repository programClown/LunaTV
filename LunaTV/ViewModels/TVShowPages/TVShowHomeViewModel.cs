using System.Diagnostics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public class TVShowHomeViewModel : ViewModelBase
{
}

public partial class MovieCardItem : ViewModelBase
{
    [ObservableProperty] private string? _name;

    [ObservableProperty] private Bitmap? _image;

    [ObservableProperty] private double _score;


    [RelayCommand]
    private void Search(string name)
    {
        Debug.Write(name);
    }
}