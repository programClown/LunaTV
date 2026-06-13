#if ANDROID
using LunaTV.Base.Models;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels.TVShowPages;

/// <summary>
/// Helper to navigate to the Android video player from any ViewModel.
/// </summary>
public static class AndroidVideoPlayerHelper
{
    public static async void Play(string mediaUrl, string title, ViewHistory? viewHistory)
    {
        var mainViewModel = App.Services.GetRequiredService<MainViewModel>();
        var previousPage = mainViewModel.PageContent;

        var playerView = new AndroidVideoPlayerView();
        mainViewModel.PageContent = playerView;

        await playerView.PlayAsync(mediaUrl, title, viewHistory, () =>
        {
            // Restore previous page when player closes
            mainViewModel.PageContent = previousPage;
        });
    }
}
#endif
