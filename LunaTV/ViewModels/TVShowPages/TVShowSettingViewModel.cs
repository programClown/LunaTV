using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.Models;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSettingViewModel : ViewModelBase
{
    private readonly SugarRepository<ApiSource> _apiSourceTable;
    private readonly HttpClient _httpClient;
    private readonly SugarRepository<PlayerConfig> _playConfigTable;
    [ObservableProperty] private ObservableCollection<ApiSourceItem> _adultApis;
    [ObservableProperty] private ObservableCollection<ApiCustomItem> _apiCustoms;
    [ObservableProperty] private ObservableCollection<ApiNetItem> _apiNets;
    [ObservableProperty] private ObservableCollection<ApiSourceItem> _commonApis;

    [ObservableProperty] private bool _doubanApiEnabled;
    [ObservableProperty] private bool _forceBaseApiNeedChecked;
    [ObservableProperty] private bool _homeAutoLoadDoubanEnabled;

    private string? _pinCode;
    [ObservableProperty] private string _pinCodeIndicator = "身形";
    [ObservableProperty] private int _selectedApiCount;

    public TVShowSettingViewModel()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // 设置超时时间

        _apiSourceTable = App.Services.GetRequiredService<SugarRepository<ApiSource>>();
        _playConfigTable = App.Services.GetRequiredService<SugarRepository<PlayerConfig>>();
        SystemSettings = App.Services.GetRequiredService<SettingsView>();

        CommonApis = new ObservableCollection<ApiSourceItem>();
        AdultApis = new ObservableCollection<ApiSourceItem>();
        ApiNets = new ObservableCollection<ApiNetItem>();
        ApiCustoms = new ObservableCollection<ApiCustomItem>();

        Dispatcher.UIThread.InvokeAsync(async () => { await NetworkTest(); });
        RefreshSource();
    }

    public object SystemSettings { get; set; }

    private void RefreshSource()
    {
        DoubanApiEnabled = AppConifg.PlayerConfig.DoubanApiEnabled;
        HomeAutoLoadDoubanEnabled = AppConifg.PlayerConfig.HomeAutoLoadDoubanEnabled;
        ForceBaseApiNeedChecked = AppConifg.PlayerConfig.ForceApiNeedSpecialSource;

        var apiSources = _apiSourceTable.GetList();
        var index = 0;
        var netIndex = 0;
        CommonApis.Clear();
        AdultApis.Clear();
        ApiNets.Clear();
        ApiCustoms.Clear();
        foreach (var api in apiSources)
        {
            index += api.IsEnable ? 1 : 0;
            if (api.IsAdult)
            {
                AdultApis.Add(new ApiSourceItem
                {
                    Id = api.Id,
                    Source = api.Source,
                    Name = api.Name,
                    Enable = api.IsEnable,
                    IsCustom = api.IsCustomApi
                });
            }
            else
            {
                CommonApis.Add(new ApiSourceItem
                {
                    Id = api.Id,
                    Source = api.Source,
                    Name = api.Name,
                    Enable = api.IsEnable,
                    IsCustom = api.IsCustomApi
                });
            }

            ApiNets.Add(new ApiNetItem
            {
                Id = api.Id,
                IndexId = ++netIndex,
                Name = api.Name,
                Url = api.ApiBaseUrl,
                IsAdult = api.IsAdult
            });

            if (api.IsCustomApi)
            {
                ApiCustoms.Add(new ApiCustomItem
                {
                    Id = api.Id,
                    Source = api.Source,
                    Name = api.Name,
                    IsAdult = api.IsAdult
                });
            }
        }

        SelectedApiCount = index;
        SyncSelectedApis();
    }

    private void SyncSelectedApis()
    {
        AppConifg.SelectApis.Clear();
        AppConifg.SelectAdultApis.Clear();
        foreach (var api in CommonApis)
            if (api.Enable)
                AppConifg.SelectApis.Add(api.Source);
        foreach (var api in AdultApis)
            if (api.Enable)
                AppConifg.SelectAdultApis.Add(api.Source);
    }

    [RelayCommand]
    private void SelectApi(ApiSourceItem api)
    {
        _apiSourceTable.Update(it => new ApiSource
        {
            IsEnable = api.Enable
        }, it => it.Id == api.Id);

        SelectedApiCount += api.Enable ? 1 : -1;
        SyncSelectedApis();
    }

    [RelayCommand]
    private void SelectAdultApi(ApiSourceItem api)
    {
        _apiSourceTable.Update(it => new ApiSource
        {
            IsEnable = api.Enable
        }, it => it.Id == api.Id);

        SelectedApiCount += api.Enable ? 1 : -1;
        SyncSelectedApis();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var api in CommonApis)
        {
            if (api.Enable) continue;
            api.Enable = true;
            _apiSourceTable.Update(it => new ApiSource
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        foreach (var api in AdultApis)
        {
            if (api.Enable) continue;
            api.Enable = true;
            _apiSourceTable.Update(it => new ApiSource
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        SelectedApiCount = CommonApis.Count + AdultApis.Count;
        SyncSelectedApis();
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var api in CommonApis)
        {
            if (!api.Enable) continue;
            api.Enable = false;
            _apiSourceTable.Update(it => new ApiSource
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        foreach (var api in AdultApis)
        {
            if (!api.Enable) continue;
            api.Enable = false;
            _apiSourceTable.Update(it => new ApiSource
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        SelectedApiCount = 0;
        SyncSelectedApis();
    }

    [RelayCommand]
    private void SelectCommonApi()
    {
        foreach (var api in CommonApis)
        {
            if (api.Enable) continue;
            api.Enable = true;
            _apiSourceTable.Update(it => new ApiSource
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        foreach (var api in AdultApis)
        {
            if (!api.Enable) continue;
            api.Enable = false;
            _apiSourceTable.Update(it => new ApiSource
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        SelectedApiCount = CommonApis.Count;
        SyncSelectedApis();
    }

    [RelayCommand]
    private async Task ExportSettings()
    {
        var filePath = await App.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "导出配置文件",
                DefaultExtension = ".json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON 文件")
                    {
                        Patterns = new[] { "*.json" },
                        MimeTypes = new[] { "application/json" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*" }
                    }
                },
                SuggestedFileName = "lunatv-settings.json"
            });

        if (filePath != null)
        {
            var apiSources = await _apiSourceTable.GetListAsync();

            var apiSourcesDict = new Dictionary<string, object>();
            apiSourcesDict.Add("Version", typeof(App).Assembly.GetName().Version?.ToString());
            apiSourcesDict.Add("ApiSource", apiSources);
            apiSourcesDict.Add("PlayerConfig", AppConifg.PlayerConfig);

            //中文序列化
            var settings = JsonSerializer.Serialize(apiSourcesDict, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });

            // 写入文件
            await using var stream = await filePath.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(settings);
        }
    }

    [RelayCommand]
    private async Task ImportSettings()
    {
        var filePath = await App.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "导出配置文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON 文件")
                    {
                        Patterns = new[] { "*.json" },
                        MimeTypes = new[] { "application/json" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            });

        if (filePath is { Count: > 0 })
        {
            var file = filePath[0];
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var settings = await reader.ReadToEndAsync();
            var apiSourcesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(settings);
            if (apiSourcesDict == null) return;
            if (apiSourcesDict.TryGetValue("ApiSource", out var apiSourcesObj))
            {
                await _apiSourceTable.AsDeleteable().Where(i => i.Id != 0).ExecuteCommandAsync();

                var apiSources = JsonSerializer.Deserialize<List<ApiSource>>(apiSourcesObj.ToString());
                if (apiSources == null) return;
                foreach (var apiSource in apiSources) await _apiSourceTable.InsertAsync(apiSource);
            }

            if (apiSourcesDict.TryGetValue("Version", out var versionObj))
            {
                var version = versionObj.ToString();
            }

            if (apiSourcesDict.TryGetValue("PlayerConfig", out var playerConfigObj))
            {
                var playerConfig = JsonSerializer.Deserialize<PlayerConfig>(playerConfigObj.ToString() ?? string.Empty);
                if (playerConfig is not null)
                {
                    var existingPlayerConfig = await _playConfigTable.GetSingleAsync(config => config.Id > 0);
                    if (existingPlayerConfig is not null)
                    {
                        playerConfig.Id = existingPlayerConfig.Id;
                        await _playConfigTable.UpdateAsync(playerConfig);
                    }
                    else
                    {
                        playerConfig.Id = await _playConfigTable.Context.Insertable(playerConfig).ExecuteReturnIdentityAsync();
                    }

                    AppConifg.PlayerConfig = playerConfig;
                }
            }

            var apiSources1 = await _apiSourceTable.GetListAsync();
            AppConifg.UpdateSites(apiSources1);
            RefreshSource();
        }
    }

    [RelayCommand]
    private async Task AddCustomApi()
    {
        TVShowAddCustomApiViewModel addCustomApiViewModel = new();
        var options = new DialogOptions
        {
            Title = "",
            Mode = DialogMode.None,
            Button = DialogButton.OKCancel,
            ShowInTaskBar = false,
            IsCloseButtonVisible = true,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanDragMove = true,
            CanResize = false,
            StyleClass = ""
        };


        var result =
#if ANDROID
            // On Android, show the add-API view as a page navigation
            await ShowAddApiViewAsPage(addCustomApiViewModel);
#else
            await Dialog.ShowModal<TVShowAddCustomApiView, TVShowAddCustomApiViewModel>(addCustomApiViewModel,
                options: options);
#endif
        if (result == DialogResult.OK)
        {
            if (addCustomApiViewModel.ApiSourceErrorVisible ||
                addCustomApiViewModel.ApiBaseUrlErrorVisible ||
                addCustomApiViewModel.ApiNameErrorVisible)
            {
                App.Notification.Show(new Notification("错误", "请把信息填完", NotificationType.Error), NotificationType.Error);
                return;
            }


            var check = await _apiSourceTable.GetSingleAsync(s => s.Source == addCustomApiViewModel.ApiSource
                                                                  && s.Name == addCustomApiViewModel.ApiName &&
                                                                  s.ApiBaseUrl == addCustomApiViewModel.ApiBaseUrl);
            if (check is not null)
            {
                App.Notification.Show(new Notification("错误", "重复添加", NotificationType.Error), NotificationType.Error);
                return;
            }

            if (await _apiSourceTable.InsertAsync(new ApiSource
                {
                    Source = addCustomApiViewModel.ApiSource,
                    ApiBaseUrl = addCustomApiViewModel.ApiBaseUrl,
                    DetailBaseUrl = addCustomApiViewModel.DetailBaseUrl,
                    Name = addCustomApiViewModel.ApiName,
                    IsAdult = addCustomApiViewModel.IsAdult,
                    IsCustomApi = true,
                    IsEnable = false
                }))
            {
                App.Notification.Show(new Notification("成功", "添加新的自定义源成功", NotificationType.Success),
                    NotificationType.Success);
                RefreshSource();
                var apiSources = await _apiSourceTable.GetListAsync();
                AppConifg.UpdateSites(apiSources);
            }
        }
    }

    [RelayCommand]
    private async Task DeleteCustomApi(ApiCustomItem api)
    {
        await _apiSourceTable.DeleteAsync(s => s.Id == api.Id);
        RefreshSource();
        var apiSources = await _apiSourceTable.GetListAsync();
        AppConifg.UpdateSites(apiSources);
    }

    [RelayCommand]
    private async Task NetworkTest()
    {
        foreach (var apiNet in ApiNets)
        {
            var latency = await PingUrlAsync(apiNet.Url);
            apiNet.Latency = latency;
        }
    }

    [RelayCommand]
    private void PinCodeConfirm()
    {
        Dispatcher.UIThread.InvokeAsync(async () => await LoadSourceFromCloud());
    }

    private async Task LoadSourceFromCloud()
    {
        try
        {
            var cloudUrl = _pinCode == "9527"
                ? "https://pz.v88.qzz.io/?format=0&source=full"
                : "https://pz.v88.qzz.io?format=0&source=jin18";
            var response = await _httpClient.GetAsync(cloudUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"Response: {jsonString}");

                // 解析JSON响应
                var cloudData = JsonSerializer.Deserialize<CloudApiSourceResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cloudData != null)
                {
                    var cloudApiSources = new List<ApiSource>();
                    // 处理云端数据
                    foreach (var (source, site) in cloudData.ApiSite)
                    {
                        cloudApiSources.Add(new ApiSource
                        {
                            Source = source,
                            ApiBaseUrl = site.Api,
                            DetailBaseUrl = site.Api.StartsWith(site.Detail) ? null : site.Detail,
                            Name = site.Name,
                            IsAdult = site.Name.Contains("🔞"),
                            IsCustomApi = false,
                            IsEnable = false
                        });
                    }

                    // _apiSourceTable全部删除后插入
                    // 删除所有API源数据后插入
                    await _apiSourceTable.AsDeleteable().Where(s => true).ExecuteCommandAsync(); // 删除所有记录
                    await _apiSourceTable.InsertRangeAsync(cloudApiSources);
                    RefreshSource();
                    var apiSources1 = await _apiSourceTable.GetListAsync();
                    AppConifg.UpdateSites(apiSources1);
                }
            }
            else
            {
                Trace.WriteLine($"HTTP请求失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"加载云端源失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PinCodeComplete(IList<string>? obj)
    {
        if (obj is null) return;
        _pinCode = string.Join("", obj);

        PinCodeIndicator = _pinCode == "9527" ? "鹤形" : "身形";
    }

    private async Task<string> PingUrlAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return "N/A";
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();
            if (response.IsSuccessStatusCode)
            {
                return $"{stopwatch.ElapsedMilliseconds}ms";
            }

            return "N/A";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ping failed for {url}: {ex.Message}");
            return "N/A";
        }
    }

    private void SavePlayerConfig()
    {
        if (AppConifg.PlayerConfig.Id > 0)
        {
            _playConfigTable.Update(AppConifg.PlayerConfig);
            return;
        }

        AppConifg.PlayerConfig.Id = _playConfigTable.Context.Insertable(AppConifg.PlayerConfig).ExecuteReturnIdentity();
    }

    partial void OnDoubanApiEnabledChanged(bool value)
    {
        AppConifg.PlayerConfig.DoubanApiEnabled = value;
        SavePlayerConfig();
    }

    partial void OnHomeAutoLoadDoubanEnabledChanged(bool value)
    {
        AppConifg.PlayerConfig.HomeAutoLoadDoubanEnabled = value;
        SavePlayerConfig();
    }

    partial void OnForceBaseApiNeedCheckedChanged(bool value)
    {
        AppConifg.PlayerConfig.ForceApiNeedSpecialSource = value;
        SavePlayerConfig();
    }

#if ANDROID
    private async Task<DialogResult> ShowAddApiViewAsPage(TVShowAddCustomApiViewModel vm)
    {
        // On Android, show as a simple page overlay with a confirm action
        // Since we can't do proper modal flow, show the view and wait
        var mainViewModel = App.Services.GetRequiredService<MainViewModel>();
        var prevPage = mainViewModel.PageContent;

        var view = new LunaTV.Views.TVShowPages.TVShowAddCustomApiView { DataContext = vm };

        // Wrap in a panel with confirm/cancel buttons
        var panel = new Avalonia.Controls.StackPanel();
        var btnPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 10),
            Spacing = 20
        };

        var tcs = new TaskCompletionSource<DialogResult>();
        var confirmBtn = new Avalonia.Controls.Button { Content = "确认添加", MinWidth = 100 };
        var cancelBtn = new Avalonia.Controls.Button { Content = "取消", MinWidth = 100 };
        confirmBtn.Click += (_, _) => { mainViewModel.PageContent = prevPage; tcs.TrySetResult(DialogResult.OK); };
        cancelBtn.Click += (_, _) => { mainViewModel.PageContent = prevPage; tcs.TrySetResult(DialogResult.None); };
        btnPanel.Children.Add(confirmBtn);
        btnPanel.Children.Add(cancelBtn);

        panel.Children.Add(view);
        panel.Children.Add(btnPanel);
        var wrapper = new Avalonia.Controls.UserControl
        {
            Content = new Avalonia.Controls.ScrollViewer { Content = panel, Padding = new Avalonia.Thickness(20) }
        };
        mainViewModel.PageContent = wrapper;

        return await tcs.Task;
    }
#endif
}

public partial class ApiSourceItem : ObservableObject
{
    [ObservableProperty] private bool _enable;
    public int Id { get; set; }
    public string? Source { get; set; }
    public string? Name { get; set; }
    public bool IsCustom { get; set; }
}

public partial class ApiNetItem : ObservableObject
{
    [ObservableProperty] private string? _latency = "待测试";

    public int IndexId { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public bool IsAdult { get; set; }
}

public class ApiCustomItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Source { get; set; }
    public bool IsAdult { get; set; }
}