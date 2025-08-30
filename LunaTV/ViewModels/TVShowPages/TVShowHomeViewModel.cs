using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Api;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowHomeViewModel : ViewModelBase
{
    private const int PageSize = 16;
    private readonly SugarRepository<ApiSource> _apiSourceTable;

    private readonly List<string> _defaultMovieTags =
        ["热门", "最新", "经典", "豆瓣高分", "冷门佳片", "华语", "欧美", "韩国", "日本", "动作", "喜剧", "日综", "爱情", "科幻", "悬疑", "恐怖", "治愈"];

    private readonly List<string> _defaultTvTags =
        ["热门", "美剧", "英剧", "韩剧", "日剧", "国产剧", "港剧", "日本动画", "综艺", "纪录片"];

    private readonly bool _initialized;

    private readonly LoadingWaitViewModel _loadingWaitViewModel = new();

    [ObservableProperty] private ObservableCollection<string> _doubanTags;
    private bool _isTagChanged2Refresh; //标签改变的时候要不要更新

    [ObservableProperty] private ObservableCollection<MovieCardItem> _movieCardItems;
    [ObservableProperty] private bool _movieChecked = true;
    private int _pageStart;
    [ObservableProperty] private string? _searchInputText;
    [ObservableProperty] private string? _selectedTagItem;
    private string _switchMovieOrTv = "movie";

    public TVShowHomeViewModel()
    {
        var pcfg = App.Services.GetRequiredService<SugarRepository<PlayerConfig>>().GetSingle(u => u.Id > 0);
        AppConifg.PlayerConfig =
            pcfg ??
            new PlayerConfig
            {
                AdFilteringEnabled = true,
                DoubanApiEnabled = false,
                HomeAutoLoadDoubanEnabled = false,
                ForceApiNeedSpecialSource = false,
                Timeout = 15000,
                FilterAds = true,
                AutoPlayNext = false
            };
        if (pcfg == null)
            App.Services.GetRequiredService<SugarRepository<PlayerConfig>>().Insert(AppConifg.PlayerConfig);

        DoubanTags = new ObservableCollection<string>();
        MovieCardItems = new ObservableCollection<MovieCardItem>();
        _ = SwitchMovieOrTv("电影");

        _initialized = true;

        _apiSourceTable = App.Services.GetRequiredService<SugarRepository<ApiSource>>();
        var apiSources = _apiSourceTable.GetList();
        AppConifg.UpdateSites(apiSources);
    }

    [RelayCommand]
    private async Task SwitchMovieOrTv(string tag)
    {
        _isTagChanged2Refresh = false;
        DoubanTags.Clear();
        if (tag == "电影")
        {
            _defaultMovieTags.ForEach(x => DoubanTags.Add(x));
            _switchMovieOrTv = "movie";
        }
        else if (tag == "电视")
        {
            _defaultTvTags.ForEach(x => DoubanTags.Add(x));
            _switchMovieOrTv = "tv";
        }

        SelectedTagItem = DoubanTags.FirstOrDefault();

        if (!_initialized && AppConifg.PlayerConfig.HomeAutoLoadDoubanEnabled)
            await RefreshMovieCardsAsync();
        else
            await RefreshMovieCardsAsync();
    }

    private async Task RefreshMovieCardsAsync()
    {
        if (SelectedTagItem is null)
        {
            _isTagChanged2Refresh = true;
            return;
        }

        if (AppConifg.PlayerConfig.DoubanApiEnabled is false)
        {
            App.Notification?.Show(new Notification("温馨提示", "豆瓣接口未启动"),
                NotificationType.Information);
            _isTagChanged2Refresh = true;
            return;
        }

        if (_initialized) _ = Loading();

        try
        {
            var sts = await App.Services.GetRequiredService<IWebApi>()
                .FetchDoubanSubjectsByTag(_switchMovieOrTv, SelectedTagItem, "recommend", PageSize,
                    _pageStart);
            var json = JsonSerializer.Deserialize<DoubanSubjectsResponse>(sts,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // 处理大小写不敏感
                });
            // Console.WriteLine(json);
            MovieCardItems.Clear();
            if (json is not null)
                foreach (var item in json.Subjects)
                {
                    var stdCover = item.Cover.Replace("\\/", "/").Replace("img2", "img3");
                    // Console.WriteLine(stdCover);
                    MovieCardItems.Add(new MovieCardItem
                    {
                        Name = item.Title,
                        Image = stdCover,
                        Score = string.IsNullOrEmpty(item.Rate) ? "暂无" : item.Rate,
                        DoubanUrl = item.Url
                    });
                }
        }
        catch (Exception e)
        {
            App.Notification?.Show(new Notification("查找失败", "豆瓣检索失败", NotificationType.Error), NotificationType.Error);
        }


        if (_initialized)
            _loadingWaitViewModel.Close();
        _isTagChanged2Refresh = true;
    }

    partial void OnSelectedTagItemChanged(string? value)
    {
        if (_isTagChanged2Refresh) RefreshMovieCardsAsync().GetAwaiter();
    }

    [RelayCommand]
    private async Task MoreMovieOrTv()
    {
        _isTagChanged2Refresh = false;
        _pageStart += PageSize;
        if (_pageStart > 9 * PageSize) _pageStart = 0;

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
        if (AppConifg.PlayerConfig.DoubanApiEnabled is false)
        {
            App.Notification?.Show(new Notification("温馨提示", "豆瓣接口未启动"),
                NotificationType.Information);
            return;
        }

        _ = Loading();

        try
        {
            var sts = await App.Services.GetRequiredService<IWebApi>()
                .GetchDoubanSearchSuggestions(text);
            var json = JsonSerializer.Deserialize<List<DoubanSuggestionSubject>>(sts,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // 处理大小写不敏感
                });
            MovieCardItems.Clear();
            if (json is not null)
                foreach (var item in json)
                {
                    var stdCover = item.Img.Replace("\\/", "/").Replace("img2", "img3");
                    // Console.WriteLine(stdCover);
                    MovieCardItems.Add(new MovieCardItem
                    {
                        Name = item.Title,
                        Image = stdCover,
                        Score = "暂无",
                        DoubanUrl = item.Url
                    });
                }
        }
        catch (Exception e)
        {
            App.Notification?.Show(new Notification("查找失败", "豆瓣检索失败", NotificationType.Error), NotificationType.Error);
        }


        _loadingWaitViewModel.Close();
    }

    [RelayCommand]
    private void NaviHistory()
    {
        var mvm = App.Services.GetRequiredService<MainViewModel>();
        if (mvm.Pages[0] is TVShowViewModel tvvm)
            tvvm.SelectedItem = tvvm.Items[2];
    }

    public async Task Loading()
    {
        var options = new DialogOptions
        {
            Title = "",
            Mode = DialogMode.None,
            Button = DialogButton.None,
            ShowInTaskBar = false,
            IsCloseButtonVisible = true,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanDragMove = true,
            CanResize = false,
            StyleClass = ""
        };

        _loadingWaitViewModel.TimerStart();

        await Dialog.ShowModal<LoadingWaitView, LoadingWaitViewModel>(_loadingWaitViewModel, options: options);
    }
}

public partial class MovieCardItem : ViewModelBase
{
    [ObservableProperty] private string? _doubanUrl;

    [ObservableProperty] private string? _image;
    [ObservableProperty] private string? _name;

    [ObservableProperty] private string? _score;


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
                svm.IsAdultMode = false;
                svm.Search(name);
            }
        }
    }
}