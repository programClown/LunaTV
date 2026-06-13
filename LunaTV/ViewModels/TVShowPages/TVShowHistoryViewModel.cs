using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Extensions;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHistoryViewModel : ViewModelBase
{
    private readonly SugarRepository<ViewHistory> _viewHistoryTable;
    private readonly List<HistoryItems> _historyItems = new();
    private bool _isUpdatingHistorySelection;
    [ObservableProperty] private ObservableCollection<HistoryItems> _allHistoryItems;
    [ObservableProperty] private string? _historyFilterText;
    [ObservableProperty] private bool _isAllHistorySelected;
    [ObservableProperty] private int _selectedHistoryCount;

    public TVShowHistoryViewModel()
    {
        AllHistoryItems = new ObservableCollection<HistoryItems>();
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
        RefreshHistoryItems();
    }

    partial void OnHistoryFilterTextChanged(string? value)
    {
        ApplyHistoryFilter();
    }

    private void ApplyHistoryFilter()
    {
        var filterText = HistoryFilterText?.Trim();
        var items = string.IsNullOrWhiteSpace(filterText)
            ? _historyItems
            : _historyItems.Where(item => MatchesHistoryFilter(item, filterText)).ToList();

        AllHistoryItems.Clear();
        foreach (var item in items)
        {
            AllHistoryItems.Add(item);
        }
        RefreshSelectedHistoryState();
    }

    private static bool MatchesHistoryFilter(HistoryItems item, string filterText)
    {
        return ContainsIgnoreCase(item.Title, filterText)
               || ContainsIgnoreCase(item.Episode, filterText)
               || ContainsIgnoreCase(item.Source, filterText);
    }

    private static bool ContainsIgnoreCase(string? value, string filterText)
    {
        return value?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true;
    }

    partial void OnIsAllHistorySelectedChanged(bool value)
    {
        if (_isUpdatingHistorySelection) return;
        _isUpdatingHistorySelection = true;
        foreach (var item in AllHistoryItems)
        {
            item.IsSelected = value;
        }
        _isUpdatingHistorySelection = false;

        RefreshSelectedHistoryState();
    }

    private void RefreshSelectedHistoryState()
    {
        SelectedHistoryCount = AllHistoryItems.Count(item => item.IsSelected);
        var allVisibleSelected = AllHistoryItems.Count > 0 && SelectedHistoryCount == AllHistoryItems.Count;
        if (IsAllHistorySelected == allVisibleSelected) return;

        _isUpdatingHistorySelection = true;
        IsAllHistorySelected = allVisibleSelected;
        _isUpdatingHistorySelection = false;
    }

    private void OnHistoryItemSelectionChanged(HistoryItems item)
    {
        if (_isUpdatingHistorySelection) return;
        RefreshSelectedHistoryState();
    }

    private string TimeSpanToFriendlyTime(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    private static string BuildMetaText(ViewHistory item)
    {
        var parts = new[]
            {
                item.Episode,
                item.Source,
                item.TotalEpisodeCount > 0 ? $"共{item.TotalEpisodeCount}集" : null
            }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(" · ", parts);
    }

    public void RefreshHistoryItems()
    {
        try
        {
        _historyItems.Clear();
        var historyItems = _viewHistoryTable.AsQueryable()
            .OrderByDescending(item => item.UpdateTime)
            .ToList();
        foreach (var item in historyItems)
        {
            System.Diagnostics.Trace.WriteLine($"[HIST] LoadHistory Id={item.Id} Name={item.Name} PlaybackPosition={item.PlaybackPosition} Duration={item.Duration}");
            var historyItem = new HistoryItems
            {
                Id = item.Id,
                VodId = item.VodId,
                Title = item.Name,
                Episode = item.Episode,
                TotalEpisodes = $"共{item.TotalEpisodeCount}集",
                Source = item.Source,
                Cover = item.Cover,
                MetaText = BuildMetaText(item),
                PlaybackPosition = item.PlaybackPosition,
                Duration = item.Duration,
                LastPlayTime = item.UpdateTime,
                TimeText = $"{TimeSpanToFriendlyTime(item.PlaybackPosition)}/{TimeSpanToFriendlyTime(item.Duration)}",
                LastPlayTimeText = item.UpdateTime.ToFriendlyTime()
            };
            historyItem.SelectionChanged += OnHistoryItemSelectionChanged;
            _historyItems.Add(historyItem);
        }
        ApplyHistoryFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LunaTV] RefreshHistoryItems failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DeleteHistoryItem(int id)
    {
        _viewHistoryTable.DeleteById(id);
        RefreshHistoryItems();
    }

    [RelayCommand]
    private void DeleteSelectedHistoryItems()
    {
        var selectedIds = AllHistoryItems
            .Where(item => item.IsSelected)
            .Select(item => item.Id)
            .ToList();
        if (selectedIds.Count == 0) return;

        _viewHistoryTable.AsDeleteable().Where(item => selectedIds.Contains(item.Id)).ExecuteCommand();
        RefreshHistoryItems();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _viewHistoryTable.AsDeleteable().Where(i => i.Id != 0).ExecuteCommand();
        RefreshHistoryItems();
    }

    [RelayCommand]
    private void PlayHistoryItem(HistoryItems? value)
    {
        if (value == null) return;

        var historyItem = _viewHistoryTable.GetById(value.Id);
        if (historyItem == null) return;

#if !ANDROID
        var win = new MpvPlayerWindow();
        (App.VisualRoot as MainWindow)?.Hide();

        win.Show();
        if (win.DataContext is MpvPlayerWindowModel videoModel)
        {
            videoModel.ViewHistory = historyItem;
            videoModel.MediaUrl = historyItem.Url;
            videoModel.Title = MpvPlayerWindowModel.BuildPlayerTitle(historyItem.Name, historyItem.Episode);
            if (historyItem.IsLocal || historyItem.Source is "本地" or "下载")
            {
                videoModel.Episodes = new ObservableCollection<EpisodeSubjectItem>
                {
                    new()
                    {
                        Name = historyItem.Episode ?? historyItem.Name,
                        Url = historyItem.Url,
                        OutputFilePath = historyItem.Url,
                        IsDownloaded = true,
                        Watched = true
                    }
                };
            }
            else
            {
                videoModel.UpdateFromHistory(historyItem.Source, historyItem.VodId, historyItem.Episode);
            }
        }
#else
        // Android: play via the video player page
        var title = $"{historyItem.Name} - {historyItem.Episode}";
        AndroidVideoPlayerHelper.Play(historyItem.Url, title, historyItem);
#endif
    }
}

public partial class HistoryItems : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public event Action<HistoryItems>? SelectionChanged;

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke(this);
    }

    public int Id { get; set; }
    public string? VodId { get; set; }
    public string? Title { get; set; }
    public string? Episode { get; set; } //多少集  
    public string? TotalEpisodes { get; set; } //总集数
    public string? Source { get; set; } //来源
    public string? Cover { get; set; }
    public string? MetaText { get; set; }
    public int PlaybackPosition { get; set; } //最近播放时间
    public int Duration { get; set; } //总时间
    public string? TimeText { get; set; }
    public string? LastPlayTimeText { get; set; }
    public DateTime? LastPlayTime { get; set; }
}