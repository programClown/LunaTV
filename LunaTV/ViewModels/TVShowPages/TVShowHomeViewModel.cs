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
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHomeViewModel : ViewModelBase
{
    private readonly List<string> _defaultMovieTags =
        ["热门", "最新", "经典", "豆瓣高分", "冷门佳片", "华语", "欧美", "韩国", "日本", "动作", "喜剧", "日综", "爱情", "科幻", "悬疑", "恐怖", "治愈"];

    private readonly List<string> _defaultTvTags =
        ["热门", "美剧", "英剧", "韩剧", "日剧", "国产剧", "港剧", "日本动画", "综艺", "纪录片"];

    [ObservableProperty] private ObservableCollection<string> _doubanTags;
    [ObservableProperty] private string? _selectedTagItem;

    [ObservableProperty] private ObservableCollection<MovieCardItem> _movieCardItems;

    public TVShowHomeViewModel()
    {
        SwitchMovieOrTv("电影");

        MovieCardItems = new ObservableCollection<MovieCardItem>();
    }

    [RelayCommand]
    private async Task SwitchMovieOrTv(string tag)
    {
        if (tag == "电影")
        {
            DoubanTags = new ObservableCollection<string>(_defaultMovieTags);
        }
        else if (tag == "电视")
        {
            DoubanTags = new ObservableCollection<string>(_defaultTvTags);
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
            .FetchDoubanSubjectsByTag("movie", "战争", "recommend", page_limit: 16);
        var json = JsonSerializer.Deserialize<DoubanSubjectsResponse>(sts,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 处理大小写不敏感
            });
        // Console.WriteLine(json);
        if (json is not null)
        {
            MovieCardItems.Clear();
            foreach (var item in json.Subjects)
            {
                var stdCover = item.Cover.Replace("\\/", "/").Replace("img2", "img3");
                Console.WriteLine(stdCover);
                MovieCardItems.Add(new MovieCardItem
                {
                    Name = item.Title,
                    Image = stdCover,
                    Score = double.Parse(item.Rate),
                });
            }
        }
    }
}

public partial class MovieCardItem : ViewModelBase
{
    [ObservableProperty] private string? _name;

    [ObservableProperty] private string? _image;

    [ObservableProperty] private double _score;


    [RelayCommand]
    private void Search(string name)
    {
        Debug.Write(name);
    }
}