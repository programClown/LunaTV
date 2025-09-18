using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using M3U8Download;

namespace LunaTV.Views;

public partial class TransferEverythingView : UserControl
{
    public TransferEverythingView()
    {
        InitializeComponent();
    }

    private void PushButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 开始下载
        Task.Run(async () =>
        {
            var downloadManager = new DownloadManager();
            downloadManager.SetFFmpegPath("C:\\Users\\Austin\\Downloads\\ffmpeg.exe");
            await downloadManager.DownloadAsync("https://vod.360zyx.vip/20250708/7T2xjBRd/index.m3u8", "D:\\",
                "test");
        });
    }
}