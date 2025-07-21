using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using LunaTV.ViewModels.Base;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSearchViewModel : ViewModelBase
{
    public ObservableCollection<string> HistoryMovies { get; set; } = new();

    public TVShowSearchViewModel()
    {
        HistoryMovies = ["血海无情", "甘十九妹", "阴阳八卦"];
    }

    [RelayCommand]
    private void Search(string name)
    {
        App.Notification.Show(
            new Notification("查找", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);
    }

    [RelayCommand]
    private void DeleteHistoty(string name)
    {
        HistoryMovies.Remove(name);
    }

    [RelayCommand]
    private void ClearAllHistories()
    {
        HistoryMovies.Clear();
    }
}