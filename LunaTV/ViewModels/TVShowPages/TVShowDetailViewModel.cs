using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using LunaTV.Base.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowDetailViewModel : ViewModelBase, IDialogContext
{
    public string? VideoName { set; get; }
    public string? SourceName { set; get; }
    public string SourceNameText => $"({ApiSourceInfo.ApiSitesConfig[SourceName].Name})";
    public DetailResult VideoDetail { set; get; }

    public bool IsVideoBorderVisible { set; get; }
    public string EpisodesCountText { set; get; }


    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    private void Play(object? episode)
    {
        if (episode is not EpisodeSubject episodeSubject)
        {
            return;
        }

        Close();
    }
}