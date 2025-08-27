using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.Constants;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using LunaTV.Views;
using LunaTV.Views.TVShowPages;
using Microsoft.Extensions.DependencyInjection;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowSettingViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ApiSourceItem> _commonApis;
    [ObservableProperty] private ObservableCollection<ApiSourceItem> _adultApis;
    [ObservableProperty] private ObservableCollection<ApiNetItem> _apiNets;
    [ObservableProperty] private ObservableCollection<ApiCustomItem> _apiCustoms;
    [ObservableProperty] private int _selectedApiCount;

    [ObservableProperty] private bool _doubanApiEnabled;
    [ObservableProperty] private bool _homeAutoLoadDoubanEnabled;
    [ObservableProperty] private bool _forceBaseApiNeedChecked;

    private readonly SugarRepository<ApiSource> _apiSourceTable;
    private readonly SugarRepository<PlayerConfig> _playConfigTable;

    public TVShowSettingViewModel()
    {
        _apiSourceTable = App.Services.GetRequiredService<SugarRepository<ApiSource>>();
        _playConfigTable = App.Services.GetRequiredService<SugarRepository<PlayerConfig>>();

        CommonApis = new ObservableCollection<ApiSourceItem>();
        AdultApis = new ObservableCollection<ApiSourceItem>();
        ApiNets = new ObservableCollection<ApiNetItem>();
        ApiCustoms = new ObservableCollection<ApiCustomItem>();

        RefreshSource();
    }

    private void RefreshSource()
    {
        DoubanApiEnabled = AppConifg.PlayerConfig.DoubanApiEnabled;
        HomeAutoLoadDoubanEnabled = AppConifg.PlayerConfig.HomeAutoLoadDoubanEnabled;
        ForceBaseApiNeedChecked = AppConifg.PlayerConfig.ForceApiNeedSpecialSource;

        var apiSources = _apiSourceTable.GetList();
        int index = 0;
        int netIndex = 0;
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
                    IsCustom = api.IsCustomApi,
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
                    IsCustom = api.IsCustomApi,
                });
            }

            ApiNets.Add(new ApiNetItem
            {
                Id = api.Id,
                IndexId = ++netIndex,
                Name = api.Name,
                Url = api.ApiBaseUrl,
                IsAdult = api.IsAdult,
            });

            if (api.IsCustomApi)
            {
                ApiCustoms.Add(new ApiCustomItem()
                {
                    Id = api.Id,
                    Source = api.Source,
                    Name = api.Name,
                    IsAdult = api.IsAdult
                });
            }
        }

        SelectedApiCount = index;
    }

    [RelayCommand]
    private void SelectApi(ApiSourceItem api)
    {
        if (api.Enable)
        {
            AppConifg.SelectApis.Add(api.Source);
        }
        else
        {
            AppConifg.SelectApis.Remove(api.Source);
        }

        _apiSourceTable.Update(it => new ApiSource()
        {
            IsEnable = api.Enable
        }, it => it.Id == api.Id);

        SelectedApiCount += api.Enable ? 1 : -1;
    }

    [RelayCommand]
    private void SelectAdultApi(ApiSourceItem api)
    {
        if (api.Enable)
        {
            AppConifg.SelectAdultApis.Add(api.Source);
        }
        else
        {
            AppConifg.SelectAdultApis.Remove(api.Source);
        }

        _apiSourceTable.Update(it => new ApiSource()
        {
            IsEnable = api.Enable
        }, it => it.Id == api.Id);

        SelectedApiCount += api.Enable ? 1 : -1;
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var api in CommonApis)
        {
            if (api.Enable) continue;
            api.Enable = true;
            _apiSourceTable.Update(it => new ApiSource()
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        foreach (var api in AdultApis)
        {
            if (api.Enable) continue;
            api.Enable = true;
            _apiSourceTable.Update(it => new ApiSource()
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        SelectedApiCount = CommonApis.Count + AdultApis.Count;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var api in CommonApis)
        {
            if (!api.Enable) continue;
            api.Enable = false;
            _apiSourceTable.Update(it => new ApiSource()
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        foreach (var api in AdultApis)
        {
            if (!api.Enable) continue;
            api.Enable = false;
            _apiSourceTable.Update(it => new ApiSource()
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        SelectedApiCount = 0;
    }

    [RelayCommand]
    private void SelectCommonApi()
    {
        foreach (var api in CommonApis)
        {
            if (api.Enable) continue;
            api.Enable = true;
            _apiSourceTable.Update(it => new ApiSource()
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        foreach (var api in AdultApis)
        {
            if (!api.Enable) continue;
            api.Enable = false;
            _apiSourceTable.Update(it => new ApiSource()
            {
                IsEnable = api.Enable
            }, it => it.Id == api.Id);
        }

        SelectedApiCount = CommonApis.Count;
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
                SuggestedFileName = "lunatv-settings.json",
            });

        if (filePath != null)
        {
            var apiSources = await _apiSourceTable.GetListAsync();

            Dictionary<string, object> apiSourcesDict = new Dictionary<string, object>();
            apiSourcesDict.Add("Version", typeof(App).Assembly.GetName().Version?.ToString());
            apiSourcesDict.Add("ApiSource", apiSources);

            //中文序列化
            var settings = JsonSerializer.Serialize(apiSourcesDict, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
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
            new FilePickerOpenOptions()
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
                foreach (var apiSource in apiSources)
                {
                    await _apiSourceTable.InsertAsync(apiSource);
                }
            }

            if (apiSourcesDict.TryGetValue("Version", out var versionObj))
            {
                var version = versionObj.ToString();
            }

            var apiSources1 = await _apiSourceTable.GetListAsync();
            AppConifg.UpdateSites(apiSources1);
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
            StyleClass = "",
        };


        var result =
            await Dialog.ShowModal<TVShowAddCustomApiView, TVShowAddCustomApiViewModel>(addCustomApiViewModel,
                options: options);
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

            if (await _apiSourceTable.InsertAsync(new ApiSource()
                {
                    Source = addCustomApiViewModel.ApiSource,
                    ApiBaseUrl = addCustomApiViewModel.ApiBaseUrl,
                    DetailBaseUrl = addCustomApiViewModel.DetailBaseUrl,
                    Name = addCustomApiViewModel.ApiName,
                    IsAdult = addCustomApiViewModel.IsAdult,
                    IsCustomApi = true,
                    IsEnable = false,
                }))
            {
                App.Notification.Show(new Notification("成功", "添加新的自定义源成功", NotificationType.Success),
                    NotificationType.Success);
                RefreshSource();
            }
        }
    }

    [RelayCommand]
    private async Task DeleteCustomApi(ApiCustomItem api)
    {
        await _apiSourceTable.DeleteAsync(s => s.Id == api.Id);
        RefreshSource();
    }

    partial void OnDoubanApiEnabledChanged(bool value)
    {
        AppConifg.PlayerConfig.DoubanApiEnabled = value;
        _playConfigTable.Update(AppConifg.PlayerConfig);
    }

    partial void OnHomeAutoLoadDoubanEnabledChanged(bool value)
    {
        AppConifg.PlayerConfig.HomeAutoLoadDoubanEnabled = value;
        _playConfigTable.Update(AppConifg.PlayerConfig);
    }

    partial void OnForceBaseApiNeedCheckedChanged(bool value)
    {
        AppConifg.PlayerConfig.ForceApiNeedSpecialSource = value;
        _playConfigTable.Update(AppConifg.PlayerConfig);
    }
}

public partial class ApiSourceItem : ObservableObject
{
    public int Id { get; set; }
    public string? Source { get; set; }
    public string? Name { get; set; }
    [ObservableProperty] private bool _enable;
    public bool IsCustom { get; set; }
}

public class ApiNetItem
{
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