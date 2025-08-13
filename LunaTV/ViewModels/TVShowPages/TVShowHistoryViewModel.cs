using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHistoryViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<HistoryItems> _allHistoryItems;

    public TVShowHistoryViewModel()
    {
        AllHistoryItems = new ObservableCollection<HistoryItems>();
        for (int i = 0; i < 100; i++)
        {
            AllHistoryItems.Add(new HistoryItems()
            {
                Title = "测试" + i,
                Episode = "第" + i + "集",
                TotalEpisodes = "100集",
                Source = "测试来源",
                CurrentTime = new TimeSpan(0, 0, i * 10),
                TotalTime = new TimeSpan(0, 0, 100 * 10),
                DateTime = DateTime.Now.AddDays(-i)
            });
        }
    }
}

public class HistoryItems
{
    public string Title { get; set; }
    public string Episode { get; set; } //多少集
    public string TotalEpisodes { get; set; } //总集数
    public string Source { get; set; } //来源
    public TimeSpan CurrentTime { get; set; } //当前时间
    public TimeSpan TotalTime { get; set; } //总时间
    public DateTime DateTime { get; set; } //最近播放时间
}