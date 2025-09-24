using System;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public class TVDownloadViewModel : ViewModelBase
{
    public DataGridCollectionView MediaDownloads { get; set; }
    public ObservableCollection<MediaDownloadViewModel> MediaDownloadViewModels { get; set; }

    public TVDownloadViewModel()
    {
        MediaDownloadViewModels = new ObservableCollection<MediaDownloadViewModel>()
        {
            new MediaDownloadViewModel()
            {
                Id = 1,
                SourceName = "TVShow",
                Name = "Test",
                Episode = "1",
                Url = "https://www.bilibili.com/video/BV12J41137hu",
                LocalPath = "D:\\Downloads\\Test.mp4",
                IsDownloaded = true,
                UpdateTime = DateTime.Now,
            }
        };
        MediaDownloads = new DataGridCollectionView(MediaDownloadViewModels);
    }
}

public partial class MediaDownloadViewModel : ObservableObject
{
    public int Id { get; set; }

    public string? SourceName { get; set; } //来源
    public string? Name { get; set; } //电影名
    public string? Episode { get; set; } //剧集
    public string? Url { get; set; } //播放地址
    public string? LocalPath { get; set; } // 本地地址
    [ObservableProperty] private bool _isDownloaded; // 是否下载完成
    public DateTime UpdateTime { get; set; }
}