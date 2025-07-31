using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LunaTV.Base.Models;
using Microsoft.Extensions.DependencyInjection;
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

        var sh = new SearchHistory
        {
            MovieName = "xuehaiqingshen",
            CreateTime = DateTime.Now,
        };

        var freesql =
            ServiceLocator.Host.Services.GetRequiredService<IFreeSql>();
        freesql.Insert<SearchHistory>(sh).ExecuteAffrows();
    }
}