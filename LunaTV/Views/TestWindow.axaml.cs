using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LunaTV.Base.Constants;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
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

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        // NotificationManager.Position = NotificationPosition.TopCenter;
        //
        // NotificationManager.Show(new Notification("哈哈", "niubi"), NotificationType.Success);
        Dispatcher.UIThread.Invoke(async () => await MessageBox.ShowAsync(this, "da1231", "1231"));

        var sugarRepository = App.Services.GetRequiredService<SugarRepository<SearchHistory>>();
        sugarRepository.InsertOrUpdate(new SearchHistory()
        {
            Id = 1,
            MovieName = "血海神抽",
            CreateTime = DateTime.Now,
        });

        Console.Write(ApiSourceInfo.ApiSitesConfig.Count);
    }
}