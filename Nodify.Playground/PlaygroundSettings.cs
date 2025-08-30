namespace Nodify.Playground;

public class PlaygroundSettings : ObservableObject
{
    private readonly IReadOnlyCollection<ISettingViewModel> _settings;

    private bool _asyncLoading = true;

    private bool _customConnectors = true;

    private bool _disableMinimapControls;

    private EditorGesturesMappings _editorGesturesMappings;

    private EditorInputMode _editorInputMode;

    private uint _maxConnectors = 4;

    private uint _maxNodes = 100;

    private uint _minConnectors;

    private PointEditor _minimapViewportOffset = new Size(2000, 2000);

    private uint _minNodes = 10;

    private uint _performanceTestNodes = 1000;

    private bool _resizeToViewport;

    private string? _searchText;

    private bool _shouldConnectNodes = true;

    private bool _showGridLines = true;

    private bool _showMinimap = true;

    private PlaygroundSettings()
    {
        _settings = new List<ISettingViewModel>
        {
            new ProxySettingViewModel<EditorGesturesMappings>(
                () => Instance.EditorGesturesMappings,
                val => Instance.EditorGesturesMappings = val,
                "Editor input mappings"),
            new ProxySettingViewModel<EditorInputMode>(
                () => Instance.EditorInputMode,
                val => Instance.EditorInputMode = val,
                "Editor input mode"),
            new ProxySettingViewModel<bool>(
                () => Instance.ShowMinimap,
                val => Instance.ShowMinimap = val,
                "Show minimap",
                "Set Enable nodes dragging optimization to false for realtime updates"),
            new ProxySettingViewModel<bool>(
                () => Instance.DisableMinimapControls,
                val => Instance.DisableMinimapControls = val,
                "Disable minimap controls",
                "Whether the minimap can move and zoom the viewport"),
            new ProxySettingViewModel<bool>(
                () => Instance.ResizeToViewport,
                val => Instance.ResizeToViewport = val,
                "Minimap resize to viewport",
                "Whether the minimap should resized to also display the viewport"),
            new ProxySettingViewModel<PointEditor>(
                () => Instance.MinimapMaxViewportOffset,
                val => Instance.MinimapMaxViewportOffset = val,
                "Minimap max viewport offset",
                "The max position from the items extent that the viewport can move to"),
            new ProxySettingViewModel<bool>(
                () => Instance.ShowGridLines,
                val => Instance.ShowGridLines = val,
                "Show grid lines:"),
            new ProxySettingViewModel<bool>(
                () => Instance.ShouldConnectNodes,
                val => Instance.ShouldConnectNodes = val,
                "Connect nodes:"),
            new ProxySettingViewModel<bool>(
                () => Instance.AsyncLoading,
                val => Instance.AsyncLoading = val,
                "Async loading:"),
            new ProxySettingViewModel<bool>(
                () => Instance.UseCustomConnectors,
                val => Instance.UseCustomConnectors = val,
                "Custom connectors:"),
            new ProxySettingViewModel<uint>(
                () => Instance.MinNodes,
                val => Instance.MinNodes = val,
                "Min nodes:"),
            new ProxySettingViewModel<uint>(
                () => Instance.MaxNodes,
                val => Instance.MaxNodes = val,
                "Max nodes:"),
            new ProxySettingViewModel<uint>(
                () => Instance.MinConnectors,
                val => Instance.MinConnectors = val,
                "Min connectors:"),
            new ProxySettingViewModel<uint>(
                () => Instance.MaxConnectors,
                val => Instance.MaxConnectors = val,
                "Max connectors:"),
            new ProxySettingViewModel<uint>(
                () => Instance.PerformanceTestNodes,
                val => Instance.PerformanceTestNodes = val,
                "Performance test nodes:")
        };
    }

    public IEnumerable<ISettingViewModel> Settings => FilterAndSort(_settings);

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value)
            .Then(() => OnPropertyChanged(nameof(Settings)));
    }

    public static PlaygroundSettings Instance { get; } = new();

    public EditorGesturesMappings EditorGesturesMappings
    {
        get => _editorGesturesMappings;
        set => SetProperty(ref _editorGesturesMappings, value)
            .Then(() => EditorGestures.Mappings.Apply(value));
    }

    public EditorInputMode EditorInputMode
    {
        get => _editorInputMode;
        set => SetProperty(ref _editorInputMode, value)
            .Then(() => EditorGestures.Mappings.Apply(value));
    }

    public bool ShowMinimap
    {
        get => _showMinimap;
        set => SetProperty(ref _showMinimap, value);
    }

    public bool DisableMinimapControls
    {
        get => _disableMinimapControls;
        set => SetProperty(ref _disableMinimapControls, value);
    }

    public bool ResizeToViewport
    {
        get => _resizeToViewport;
        set => SetProperty(ref _resizeToViewport, value);
    }

    public PointEditor MinimapMaxViewportOffset
    {
        get => _minimapViewportOffset;
        set => SetProperty(ref _minimapViewportOffset, value);
    }

    public bool ShouldConnectNodes
    {
        get => _shouldConnectNodes;
        set => SetProperty(ref _shouldConnectNodes, value);
    }

    public bool AsyncLoading
    {
        get => _asyncLoading;
        set => SetProperty(ref _asyncLoading, value);
    }

    public uint MinNodes
    {
        get => _minNodes;
        set => SetProperty(ref _minNodes, value)
            .Then(() => MaxNodes = MaxNodes < MinNodes ? MinNodes : MaxNodes);
    }

    public uint MaxNodes
    {
        get => _maxNodes;
        set => SetProperty(ref _maxNodes, value)
            .Then(() => MaxNodes = MaxNodes < MinNodes ? MinNodes : MaxNodes);
    }

    public uint MinConnectors
    {
        get => _minConnectors;
        set => SetProperty(ref _minConnectors, value)
            .Then(() => MaxConnectors = MaxConnectors < MinConnectors ? MinConnectors : MaxConnectors);
    }

    public uint MaxConnectors
    {
        get => _maxConnectors;
        set => SetProperty(ref _maxConnectors, value)
            .Then(() => MaxConnectors = MaxConnectors < MinConnectors ? MinConnectors : MaxConnectors);
    }

    public uint PerformanceTestNodes
    {
        get => _performanceTestNodes;
        set => SetProperty(ref _performanceTestNodes, value);
    }

    public bool ShowGridLines
    {
        get => _showGridLines;
        set => SetProperty(ref _showGridLines, value);
    }

    public bool UseCustomConnectors
    {
        get => _customConnectors;
        set => SetProperty(ref _customConnectors, value);
    }

    public IEnumerable<ISettingViewModel> FilterAndSort(IReadOnlyCollection<ISettingViewModel> settings)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return settings;

        var searchText = SearchText!.ToLowerInvariant();

        var matchingValues = settings.Where(s =>
            s.Name.ToLowerInvariant().Contains(searchText) ||
            (s.Description?.ToLowerInvariant()?.Contains(searchText) ?? false));
        var sortedValues = matchingValues.OrderByDescending(s => s.Name.ToLowerInvariant().Contains(searchText));

        return sortedValues;
    }
}