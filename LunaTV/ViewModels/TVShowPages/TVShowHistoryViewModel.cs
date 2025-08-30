using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Extensions;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Media;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHistoryViewModel : ViewModelBase
{
    private readonly SugarRepository<ViewHistory> _viewHistoryTable;
    [ObservableProperty] private ObservableCollection<HistoryItems> _allHistoryItems;
    [ObservableProperty] private HistoryItems? _selectedHistoryItem;

    public TVShowHistoryViewModel()
    {
        AllHistoryItems = new ObservableCollection<HistoryItems>();
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
        var historyItems = _viewHistoryTable.GetList();
        foreach (var item in historyItems)
            AllHistoryItems.Add(new HistoryItems
            {
                Id = item.Id,
                VodId = item.VodId,
                Title = item.Name,
                Episode = item.Episode,
                TotalEpisodes = $"共{item.TotalEpisodeCount}集",
                Source = item.Source,
                PlaybackPosition = item.PlaybackPosition,
                Duration = item.Duration,
                LastPlayTime = item.UpdateTime,
                TimeText = $"{TimeSpanToFriendlyTime(item.PlaybackPosition)}/{TimeSpanToFriendlyTime(item.Duration)}",
                LastPlayTimeText = item.UpdateTime.ToFriendlyTime()
            });
    }

    private string TimeSpanToFriendlyTime(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    public void RefreshHistoryItems()
    {
        AllHistoryItems.Clear();
        var historyItems = _viewHistoryTable.GetList();
        foreach (var item in historyItems)
            AllHistoryItems.Add(new HistoryItems
            {
                Id = item.Id,
                VodId = item.VodId,
                Title = item.Name,
                Episode = item.Episode,
                TotalEpisodes = $"共{item.TotalEpisodeCount}集",
                Source = item.Source,
                PlaybackPosition = item.PlaybackPosition,
                Duration = item.Duration,
                LastPlayTime = item.UpdateTime,
                TimeText = $"{TimeSpanToFriendlyTime(item.PlaybackPosition)}/{TimeSpanToFriendlyTime(item.Duration)}",
                LastPlayTimeText = item.UpdateTime.ToFriendlyTime()
            });
    }

    [RelayCommand]
    private void DeleteHistoryItem(int id)
    {
        _viewHistoryTable.DeleteById(id);
        SelectedHistoryItem = null;
        RefreshHistoryItems();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _viewHistoryTable.AsDeleteable().Where(i => i.Id != 0).ExecuteCommand();
        SelectedHistoryItem = null;
        RefreshHistoryItems();
    }

    partial void OnSelectedHistoryItemChanged(HistoryItems? value)
    {
        if (value == null) return;

        var historyItem = _viewHistoryTable.GetById(value.Id);
        if (historyItem == null) return;

        var win = new PlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();

        win.Show();
        if (win.VideoPlayer.DataContext is VideoPlayerViewModel videoModel)
        {
            videoModel.VideoPath = historyItem.Url;
            videoModel.VideoName = $"{historyItem.Name} {historyItem.Episode}";
            videoModel.ViewHistory = historyItem;
            videoModel.UpdateFromHistory(historyItem.Source, historyItem.VodId, historyItem.Episode);
        }
    }
}

public class HistoryItems
{
    public int Id { get; set; }
    public string? VodId { get; set; }
    public string? Title { get; set; }
    public string? Episode { get; set; } //多少集  
    public string? TotalEpisodes { get; set; } //总集数
    public string? Source { get; set; } //来源
    public int PlaybackPosition { get; set; } //最近播放时间
    public int Duration { get; set; } //总时间
    public string? TimeText { get; set; }
    public string? LastPlayTimeText { get; set; }
    public DateTime? LastPlayTime { get; set; }
}