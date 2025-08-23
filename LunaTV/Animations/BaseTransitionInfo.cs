using System;
using FluentAvalonia.UI.Media.Animation;

namespace LunaTV.Animations;

public abstract class BaseTransitionInfo : NavigationTransitionInfo
{
    /// <summary>
    ///     The duration of the animation at 1x animation scale
    /// </summary>
    public abstract TimeSpan Duration { get; set; }
}