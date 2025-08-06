using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHomeViewModel : ViewModelBase
{
    private readonly List<string> _defaultMovieTags =
        ["热门", "最新", "经典", "豆瓣高分", "冷门佳片", "华语", "欧美", "韩国", "日本", "动作", "喜剧", "日综", "爱情", "科幻", "悬疑", "恐怖", "治愈"];

    private readonly List<string> _defaultTvTags =
        ["热门", "美剧", "英剧", "韩剧", "日剧", "国产剧", "港剧", "日本动画", "综艺", "纪录片"];

    [ObservableProperty] private ObservableCollection<string> _doubanTags;
    [ObservableProperty] private string? _selectedItem;

    [ObservableProperty] private ObservableCollection<MovieCardItem> _movieCardItems;

    public TVShowHomeViewModel()
    {
        _doubanTags = new ObservableCollection<string>(_defaultMovieTags);

        MovieCardItems = new ObservableCollection<MovieCardItem>
        {
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
            new()
            {
                Name = "国家宝藏",
                Score = 7.5,
            },
        };
    }
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