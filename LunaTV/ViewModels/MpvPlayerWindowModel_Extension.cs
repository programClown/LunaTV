using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    [ObservableProperty] private ObservableCollection<EpisodeSubjectItem> _episodes;
    private SugarRepository<ViewHistory> _viewHistoryTable;
    public ViewHistory? ViewHistory { get; set; }

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
            if (videos?.Episodes is not { Count: > 0 }) return;

            var episodes = videos.Episodes.Select(ep => new EpisodeSubjectItem
            {
                Watched = ep.Name == name,
                Name = ep.Name,
                Url = ep.Url
            }).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Episodes = new ObservableCollection<EpisodeSubjectItem>(episodes);
            });
        });
    }

    private void SaveViewHistory()
    {
        System.Diagnostics.Trace.WriteLine($"[HIST] SaveViewHistory ENTER Duration={Duration.TotalSeconds} _lastPositionValue={_lastPositionValue} ViewHistory?.Id={ViewHistory?.Id} ViewHistory==null?{ViewHistory is null}");
        if (Duration > TimeSpan.FromSeconds(1) && ViewHistory is not null)
        {
            System.Diagnostics.Trace.WriteLine($"[HIST] SaveViewHistory WRITE PlaybackPosition={(int)_lastPositionValue} Id={ViewHistory.Id} (Insert? {ViewHistory.Id==0})");
            ViewHistory.PlaybackPosition = (int)_lastPositionValue;
            ViewHistory.Duration = (int)Duration.TotalSeconds;
            ViewHistory.UpdateTime = DateTime.Now;
            if (ViewHistory.Id == 0)
            {
                ViewHistory.Id = _viewHistoryTable.InsertReturnIdentity(ViewHistory);
            }
            else
            {
                _viewHistoryTable.Update(ViewHistory);
            }
        }
        else
        {
            System.Diagnostics.Trace.WriteLine("[HIST] SaveViewHistory SKIPPED guard failed");
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
                        Title = BuildPlayerTitle(ViewHistory?.Name, Episodes[Episodes.IndexOf(episode) + 1].Name);
                        Episodes.ToList().ForEach(episode =>
                            episode.Watched = episode.Name == Episodes[Episodes.IndexOf(episode) + 1].Name);

                        return;
                    }
                }
            }
        }
    }
}