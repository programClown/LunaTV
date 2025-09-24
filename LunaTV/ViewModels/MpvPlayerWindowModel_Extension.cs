using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.Services;
using LunaTV.ViewModels.TVShowPages;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels;

public partial class MpvPlayerWindowModel
{
    private AppJsonConfig? _appJsonConfig;
    private SugarRepository<ViewHistory> _viewHistoryTable;
    public ViewHistory? ViewHistory { get; set; }
    [ObservableProperty] private ObservableCollection<EpisodeSubjectItem> _episodes;

    private void DbServiceInit()
    {
        _viewHistoryTable = App.Services.GetRequiredService<SugarRepository<ViewHistory>>();
        _appJsonConfig = App.Services.GetRequiredService<AppJsonConfigService>().ReadJson<AppJsonConfig>() ??
                         new AppJsonConfig
                         {
                             Player = new Player
                             {
                                 Vol = 50,
                                 Muted = false
                             }
                         };
    }

    public void UpdateFromHistory(string source, string vodId, string name)
    {
        Task.Run(async () =>
        {
            var videos = await App.Services.GetRequiredService<MovieTvService>()
                .SearchDetail(source, vodId, AppConifg.AdultApiSitesConfig.ContainsKey(source));
            Episodes = new ObservableCollection<EpisodeSubjectItem>(videos.Episodes.Select(ep =>
                new EpisodeSubjectItem
                {
                    Watched = ep.Name == name,
                    Name = ep.Name,
                    Url = ep.Url
                }).ToList());
        });
    }

    private void SaveViewHistory()
    {
        if (Duration > TimeSpan.FromSeconds(1) && ViewHistory is not null)
        {
            ViewHistory.PlaybackPosition = (int)Position.TotalSeconds;
            ViewHistory.Duration = (int)Duration.TotalSeconds;
            ViewHistory.UpdateTime = DateTime.Now;
            _viewHistoryTable.InsertOrUpdate(ViewHistory);
        }
    }

    private void SaveMute()
    {
        if (_appJsonConfig != null)
        {
            _appJsonConfig.Player.Muted = IsMuted;
            App.Services.GetRequiredService<AppJsonConfigService>().WriteJson(_appJsonConfig);
        }
    }

    private void SaveVolume()
    {
        if (_appJsonConfig != null)
        {
            _appJsonConfig.Player.Vol = Volume;
            App.Services.GetRequiredService<AppJsonConfigService>().WriteJson(_appJsonConfig);
        }
    }

    private void MediaPlayerOnLoaded()
    {
        Volume = (int)_appJsonConfig.Player.Vol;
        IsMuted = _appJsonConfig.Player.Muted;
    }

    private void MediaPlayerOnEndReached()
    {
        if (Episodes is { Count: > 0 })
        {
            foreach (var episode in Episodes)
            {
                if (episode.Url == MediaUrl)
                {
                    if (Episodes.Count > Episodes.IndexOf(episode) + 1)
                    {
                        ViewHistory.PlaybackPosition = 0;
                        ViewHistory.Episode = Episodes[Episodes.IndexOf(episode) + 1].Name;
                        ViewHistory.Url = Episodes[Episodes.IndexOf(episode) + 1].Url;

                        MediaUrl = Episodes[Episodes.IndexOf(episode) + 1].Url;
                        Title = $"{ViewHistory?.Name} {Episodes[Episodes.IndexOf(episode) + 1].Name}";
                        Episodes.ToList().ForEach(episode =>
                            episode.Watched = episode.Name == Episodes[Episodes.IndexOf(episode) + 1].Name);

                        return;
                    }
                }
            }
        }
    }
}