using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowFilterViewModel : ViewModelBase
{
    public List<string> TagItems => ["全部", "电影", "电视剧", "综艺", "动画", "纪录片", "短片"];
    [ObservableProperty] private string? _selectedTagItem;

    public List<string> GenresItems =>
    [
        "全部", "喜剧", "爱情", "动作", "科幻", "动画", "悬疑", "犯罪", "惊悚", "冒险", "音乐", "历史", "奇幻", "恐怖", "战争", "传记", "歌舞", "武侠",
        "情色", "灾难", "西部", "纪录片", "短片",
    ];

    [ObservableProperty] private string? _selectedGenresItem;

    public List<string> CountryItems =>
    [
        "全部", "中国大陆", "美国", "香港", "台湾", "日本", "韩国", "英国", "法国", "德国", "意大利", "西班牙", "印度", "泰国", "俄罗斯", "伊朗", "加拿大",
        "澳大利亚", "爱尔兰", "瑞典", "巴西", "丹麦",
    ];

    [ObservableProperty] private string? _selectedCountryItem;

    public List<string> SortItems =>
    [
        "按热度排序", "按时间排序", "按评分排序"
    ];
    // T R S

    [ObservableProperty] private string? _selectedSortItem;


    public ObservableCollection<MovieCardItem> MovieCardItems { set; get; }

    public TVShowFilterViewModel()
    {
        SelectedTagItem = TagItems[0];
        SelectedGenresItem = GenresItems[0];
        SelectedCountryItem = CountryItems[0];
        SelectedSortItem = SortItems[0];

        MovieCardItems = new ObservableCollection<MovieCardItem>();
        for (int i = 0; i < 100; i++)
        {
            MovieCardItems.Add(new MovieCardItem
            {
                Name = "国家宝藏",
                Score = "7.5",
            });
        }
    }
}