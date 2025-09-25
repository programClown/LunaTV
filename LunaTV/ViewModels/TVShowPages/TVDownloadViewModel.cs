using System;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
            new()
            {
                Id = 1,
                SourceName = "TVShow",
                Name = "小包与康熙",
                Episode = "第1集",
                Url = "https://www.bilibili.com/video/BV12J41137hu",
                LocalPath = "D:\\Downloads\\Test.mp4",
                IsDownloaded = true,
                UpdateTime = DateTime.Now,
                Status = "下载中"
            },
            new()
            {
                Id = 2,
                SourceName = "TVShow",
                Name = "小包与康熙",
                Episode = "第2集",
                Url = "https://www.bilibili.com/video/BV12J41137hu",
                LocalPath = "D:\\Downloads\\Test.mp4",
                IsDownloaded = true,
                UpdateTime = DateTime.Now,
                Status = "下载中"
            },
            new()
            {
                Id = 3,
                SourceName = "TVShow",
                Name = "双气镇刀客",
                Episode = "无",
                Url = "https://www.bilibili.com/video/BV12J41137hu",
                LocalPath = "D:\\Downloads\\Test.mp4",
                IsDownloaded = true,
                UpdateTime = DateTime.Now,
                Status = "下载中"
            }
        };
        MediaDownloads = new DataGridCollectionView(MediaDownloadViewModels);
        MediaDownloads.GroupDescriptions.Add(new DataGridPathGroupDescription("Name"));
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
    public bool IsDownloaded { get; set; } // 本地地址
    [ObservableProperty] private bool _actionIndicate; // 动作指示
    [ObservableProperty] private string? _actionText = "开始"; // 状态 开始/暂停/重新下载
    [ObservableProperty] private string? _status = "未开始";
    [ObservableProperty] private bool _statusIndicate; // 状态指示

    public DateTime UpdateTime { get; set; }

    partial void OnActionTextChanged(string? value)
    {
        Status = value switch
        {
            "开始" => "未开始",
            "暂停" => "下载中",
            "重新下载" => "已完成",
            _ => "未开始"
        };

        IsDownloaded = Status == "已完成";
        ActionIndicate = Status != "未开始";
        StatusIndicate = Status == "下载中";
    }

    [RelayCommand]
    private void DownloadAction()
    {
        ActionText = ActionText switch
        {
            "开始" => "暂停",
            "暂停" => "重新下载",
            "重新下载" => "开始",
            _ => "开始"
        };
    }
}