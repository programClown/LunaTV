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
            downloadManager.SetFFmpegPath("C:\\Users\\zhaom\\Downloads\\N_m3u8DL-RE\\ffmpeg.exe");
            await downloadManager.DownloadAsync("https://vip.dytt-luck.com/20250827/19419_91c53075/index.m3u8", "D:\\",
                "test.mp4");
        });
    }
}