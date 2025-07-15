using System;

namespace LunaTV.Models;

/// <summary>
///     菜单项 model
/// </summary>
public class MenuItemModel
{
    /// <summary>
    ///     菜单标题
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    ///     菜单图标
    /// </summary>
    public string Icon { get; init; }

    /// <summary>
    ///     菜单对应的页面类型
    /// </summary>
    public required Type ViewType { get; init; }
}