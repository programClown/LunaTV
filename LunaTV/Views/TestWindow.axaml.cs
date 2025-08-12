using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LunaTV.Base.Api;
using LunaTV.Base.Constants;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Services;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace LunaTV.Views;

public partial class TestWindow : UrsaWindow
{
    public TestWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        // NotificationManager.Position = NotificationPosition.TopCenter;
        //
        // NotificationManager.Show(new Notification("哈哈", "niubi"), NotificationType.Success);
        Dispatcher.UIThread.Invoke(async () => await MessageBox.ShowAsync(this, "da1231", "1231"));

        // var sugarRepository = App.Services.GetRequiredService<SugarRepository<SearchHistory>>();
        // sugarRepository.InsertOrUpdate(new SearchHistory()
        // {
        //     Id = 1,
        //     MovieName = "血海神抽",
        //     CreateTime = DateTime.Now,
        // });
        //
        // Console.Write(ApiSourceInfo.ApiSitesConfig.Count);
        // var sts = App.Services.GetRequiredService<IWebApi>()
        //     .NewApiGetchDoubanChartTopList(tags: "电影", genres: "科幻", sort: "T", range: "7,10").GetAwaiter().GetResult();
        // var sts = App.Services.GetRequiredService<IWebApi>().FetchDoubanTags("movie").GetAwaiter().GetResult();
        // var sts = App.Services.GetRequiredService<IWebApi>().FetchDoubanSubjectsByTag("movie", "战争", "recommend")
        //     .GetAwaiter().GetResult();
        // var sts = App.Services.GetRequiredService<IWebApi>().GetchDoubanSearchSuggestions("红楼梦")
        //     .GetAwaiter().GetResult();
        // Console.WriteLine(sts);

        // var api = App.Services.GetRequiredService<IApiFactory>();
        // var client = api.CreateRefitClient<IMovieTvApi>(new Uri(ApiSourceInfo.ApiSitesConfig["jisu"].ApiBaseUrl));
        // var sts = await client.SearchVideos("唐伯虎");

        var service = App.Services.GetRequiredService<MovieTvService>();
        var details = await service.SearchDetail("ffzy", "84032");
        Console.WriteLine(details);
    }
}