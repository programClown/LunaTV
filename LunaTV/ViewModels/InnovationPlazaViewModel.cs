using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using LunaTV.ViewModels.Base;
using LunaTV.ViewModels.Media;
using LunaTV.Views;
using Microsoft.Extensions.Hosting.Internal;
using Ursa.Controls;

namespace LunaTV.ViewModels;

public partial class InnovationPlazaViewModel : PageViewModelBase
{
    public override string Title => "自由创作";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorLineIcon", out var value) ? (IconSource)value : null;

    [RelayCommand]
    private async void OpenLocalVideo()
    {
        var videoAll = new FilePickerFileType("All Videos")
        {
            Patterns = new string[6] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv" },
            AppleUniformTypeIdentifiers = new string[1] { "public.video" },
            MimeTypes = new string[1] { "video/*" }
        };


        var file = await App.StorageProvider?.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "打开文件",
                FileTypeFilter = new[]
                {
                    videoAll,
                    FilePickerFileTypes.All,
                },
                AllowMultiple = false,
            });

        if (file is { Count: > 0 })
        {
            var win = new PlayerWindow();
            (App.VisualRoot as MainWindow)?.Hide();
            win.Show();
            if (win.VideoPlayer.DataContext is VideoPlayerViewModel videoModel)
            {
                videoModel.VideoPath = file[0].Path.LocalPath;
                videoModel.VideoName = file[0].Path.LocalPath.Substring(file[0].Path.LocalPath.LastIndexOf('\\') + 1);
            }
        }
    }
}