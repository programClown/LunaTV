using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using HanumanInstitute.LibMpv;
using HanumanInstitute.LibMpv.Avalonia;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels;

public partial class InnovationPlazaViewModel : PageViewModelBase
{
    [ObservableProperty] private string _mediaUrl = "https://vip.dytt-luck.com/20250827/19457_e0c4ac2b/index.m3u8";

    [ObservableProperty] public VideoRenderer _renderer;

    public override string Title => "自由创作";

    public override IconSource IconSource { set; get; } =
        App.TopLevel.TryFindResource("ColorLineIcon", out var value) ? (IconSource)value : null;

    public MpvContext Mpv { get; set; } = default!;

    public async void Play()
    {
        Stop();
        await Mpv.LoadFile(MediaUrl).InvokeAsync();
    }

    public void Pause()
    {
        Pause(null);
    }

    public void Pause(bool? value)
    {
        value ??= !Mpv.Pause.Get()!;
        Mpv.Pause.Set(value.Value);
    }

    public void Stop()
    {
        Mpv.Stop().Invoke();
        Mpv.Pause.Set(false);
    }

    public void Software()
    {
        Renderer = VideoRenderer.Software;
    }

    public void OpenGl()
    {
        Renderer = VideoRenderer.OpenGl;
    }

    public void Native()
    {
        Renderer = VideoRenderer.Native;
    }
}