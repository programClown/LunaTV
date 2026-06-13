using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.ViewModels.Base;

namespace LunaTV.ViewModels.TVShowPages;

public partial class ManageDoubanTagsViewModel : ViewModelBase
{
    [ObservableProperty] private string? _newTagInput;
    [ObservableProperty] private string? _duplicateWarning;

    public ManageDoubanTagsViewModel()
    {
        Tags = new ObservableCollection<string>();
    }

    public ObservableCollection<string> Tags { get; }

    public string TagCountText => $"共 {Tags.Count} 个标签";

    public void LoadTags(List<string> tags)
    {
        Tags.Clear();
        foreach (var tag in tags) Tags.Add(tag);
        OnPropertyChanged(nameof(TagCountText));
    }

    public List<string> GetTags() => [.. Tags];

    [RelayCommand]
    private void AddTag()
    {
        if (string.IsNullOrWhiteSpace(NewTagInput)) return;
        var tag = NewTagInput.Trim();
        if (Tags.Contains(tag))
        {
            DuplicateWarning = $"\"{tag}\" 已存在";
            return;
        }

        DuplicateWarning = null;
        Tags.Add(tag);
        NewTagInput = null;
        OnPropertyChanged(nameof(TagCountText));
    }

    public void RemoveTagAt(int index)
    {
        if (index < 0 || index >= Tags.Count) return;
        Tags.RemoveAt(index);
        DuplicateWarning = null;
        OnPropertyChanged(nameof(TagCountText));
    }

    [RelayCommand]
    private void RemoveTagByParam(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        var idx = Tags.IndexOf(tag);
        if (idx >= 0) RemoveTagAt(idx);
    }

    public void MoveTag(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Tags.Count) return;
        if (toIndex < 0 || toIndex >= Tags.Count) return;
        if (fromIndex == toIndex) return;
        Tags.Move(fromIndex, toIndex);
    }

    public List<string> DefaultMovieTags { get; set; } = [];
    public List<string> DefaultTvTags { get; set; } = [];
    public bool IsMovieMode { get; set; } = true;

    [RelayCommand]
    private void ResetToDefaultList()
    {
        Tags.Clear();
        var defaults = IsMovieMode ? DefaultMovieTags : DefaultTvTags;
        foreach (var tag in defaults) Tags.Add(tag);
        DuplicateWarning = null;
        OnPropertyChanged(nameof(TagCountText));
    }
}
