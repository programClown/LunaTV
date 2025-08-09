using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Nodify.Avalonia.Shared;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHomeViewModel : ViewModelBase
{
    private readonly List<string> _defaultMovieTags =
        ["热门", "最新", "经典", "豆瓣高分", "冷门佳片", "华语", "欧美", "韩国", "日本", "动作", "喜剧", "日综", "爱情", "科幻", "悬疑", "恐怖", "治愈"];

    private readonly List<string> _defaultTvTags =
        ["热门", "美剧", "英剧", "韩剧", "日剧", "国产剧", "港剧", "日本动画", "综艺", "纪录片"];

    [ObservableProperty] private ObservableCollection<string> _doubanTags;
    [ObservableProperty] private string? _selectedTagItem;
    [ObservableProperty] private bool _movieChecked = true;
    private string _switchMovieOrTv = "movie";
    private int _pageStart = 0;
    private const int PageSize = 16;
    private bool _isTagChanged2Refresh = false; //标签改变的时候要不要更新

    [ObservableProperty] private ObservableCollection<MovieCardItem> _movieCardItems;
    [ObservableProperty] private string? _searchInputText;

    public TVShowHomeViewModel()
    {
        DoubanTags = new ObservableCollection<string>();
        MovieCardItems = new ObservableCollection<MovieCardItem>();
        _ = SwitchMovieOrTv("电影");
    }

    [RelayCommand]
    private async Task SwitchMovieOrTv(string tag)
    {
        _isTagChanged2Refresh = false;
        DoubanTags.Clear();
        if (tag == "电影")
        {
            DoubanTags.AddRange(_defaultMovieTags);
            _switchMovieOrTv = "movie";
        }
        else if (tag == "电视")
        {
            DoubanTags.AddRange(_defaultTvTags);
            _switchMovieOrTv = "tv";
        }

        SelectedTagItem = DoubanTags.FirstOrDefault();

        await RefreshMovieCardsAsync();
    }

    private async Task RefreshMovieCardsAsync()
    {
        if (SelectedTagItem is null)
        {
            return;
        }

        var sts = await App.Services.GetRequiredService<IWebApi>()
            .FetchDoubanSubjectsByTag(_switchMovieOrTv, SelectedTagItem, "recommend", page_limit: PageSize,
                page_start: _pageStart);
        var json = JsonSerializer.Deserialize<DoubanSubjectsResponse>(sts,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 处理大小写不敏感
            });
        // Console.WriteLine(json);
        MovieCardItems.Clear();
        if (json is not null)
        {
            foreach (var item in json.Subjects)
            {
                var stdCover = item.Cover.Replace("\\/", "/").Replace("img2", "img3");
                // Console.WriteLine(stdCover);
                MovieCardItems.Add(new MovieCardItem
                {
                    Name = item.Title,
                    Image = stdCover,
                    Score = string.IsNullOrEmpty(item.Rate) ? "暂无" : item.Rate,
                    DoubanUrl = item.Url,
                });
            }
        }

        _isTagChanged2Refresh = true;
    }

    partial void OnSelectedTagItemChanged(string? value)
    {
        if (_isTagChanged2Refresh)
            RefreshMovieCardsAsync().GetAwaiter();
    }

    [RelayCommand]
    private async Task MoreMovieOrTv()
    {
        _isTagChanged2Refresh = false;
        _pageStart += PageSize;
        if (_pageStart > 9 * PageSize)
        {
            _pageStart = 0;
        }

        await RefreshMovieCardsAsync();
    }

    [RelayCommand]
    private async Task BackHome()
    {
        MovieChecked = true;
        await SwitchMovieOrTv("电影");
    }

    [RelayCommand]
    private async Task NaviSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var sts = await App.Services.GetRequiredService<IWebApi>()
            .GetchDoubanSearchSuggestions(text);
        var json = JsonSerializer.Deserialize<List<DoubanSuggestionSubject>>(sts,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 处理大小写不敏感
            });
        MovieCardItems.Clear();
        if (json is not null)
        {
            foreach (var item in json)
            {
                var stdCover = item.Img.Replace("\\/", "/").Replace("img2", "img3");
                // Console.WriteLine(stdCover);
                MovieCardItems.Add(new MovieCardItem
                {
                    Name = item.Title,
                    Image = stdCover,
                    Score = "暂无",
                    DoubanUrl = item.Url,
                });
            }
        }
    }

    [RelayCommand]
    private void NaviHistory()
    {
        var mvm = App.Services.GetRequiredService<MainViewModel>();
        if (mvm.Pages[0] is TVShowViewModel tvvm)
            tvvm.SelectedItem = tvvm.Items[2];
    }
}

public partial class MovieCardItem : ViewModelBase
{
    [ObservableProperty] private string? _name;

    [ObservableProperty] private string? _image;

    [ObservableProperty] private string? _score;
    
    [ObservableProperty] private string? _doubanUrl;


    [RelayCommand]
    private void Search(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var mvm = App.Services.GetRequiredService<MainViewModel>();
        if (mvm.Pages[0] is TVShowViewModel tvvm)
        {
            tvvm.SelectedItem = tvvm.Items[1];
            var sview = tvvm.GetControl(tvvm.SelectedItem.Name) as TVShowSearchView;
            // var svm = sview?.DataContext as TVShowSearchViewModel;
            if (sview?.DataContext is TVShowSearchViewModel svm)
            {
                svm.InputMovieTvName = name;
                svm.Search(name);
            }
        }
    }
}