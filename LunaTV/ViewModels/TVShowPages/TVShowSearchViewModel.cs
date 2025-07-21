using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using LunaTV.ViewModels.Base;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSearchViewModel : ViewModelBase
{
    public ObservableCollection<string> HistoryMovies { get; set; } = new();
    
    public ObservableCollection<SearchResult> SearchResults { get; set; } = new();
    
    public TVShowSearchViewModel()
    {
        HistoryMovies = ["血海无情", "甘十九妹", "阴阳八卦"];
        SearchResults = new ObservableCollection<SearchResult>()
            {
                new SearchResult()
                {
                    Name = "血海无情",
                    Tag = "犯罪 历史",
                    Year = 2022,
                    Cover = "avares://LunaTV/Assets/nomedia.png",
                    Descriptor = "HD",
                    MovieSource = "非凡影视"
                },
                new SearchResult()
                {
                    Name = "甘十九妹",
                    Tag = "爱情 历史",
                    Year = 2022,
                    Cover = "avares://LunaTV/Assets/nomedia.png",
                    Descriptor = "已完结",
                    MovieSource = "360影库"
                },
                new SearchResult()
                {
                    Name = "阴阳八卦",
                    Tag = "爱情 历史",
                    Year = 2022,
                    Cover = "avares://LunaTV/Assets/nomedia.png",
                    Descriptor = "暂无介绍",
                    MovieSource = "优酷网"
                },
            };
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
    
    [RelayCommand]
    private void Play(string name)
    {
        App.Notification.Show(
            new Notification("播放", name, NotificationType.Success),
            NotificationType.Success,
            showClose: true);
    }
}

public class SearchResult
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int Year { get; set; } 
    public string Cover { get; set; } = string.Empty;
    public string Descriptor { get; set; } = string.Empty;
    public string MovieSource { get; set; } = string.Empty;
}