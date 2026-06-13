using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaTV.Base.DB.UnitOfWork;
using LunaTV.Base.Models;
using LunaTV.Constants;
using LunaTV.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace LunaTV.ViewModels.TVShowPages;

public partial class TVShowDownloadViewModel : ViewModelBase
{
    private readonly SugarRepository<MediaDownload> _downloadRepository;

    private CancellationTokenSource? _cancellationTokenSource;
    [ObservableProperty] private string? _downloadName;
    private Process? _downloadProcess;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string? _downloadStatus;

    [ObservableProperty] private ObservableCollection<MediaDownload> _downloadTasks;
    [ObservableProperty] private string? _downloadUrl;
    [ObservableProperty] private bool _isDownloading;

    public TVShowDownloadViewModel()
    {
        _downloadRepository = App.Services.GetRequiredService<SugarRepository<MediaDownload>>();
        DownloadTasks = new ObservableCollection<MediaDownload>();
        DownloadStatus = "准备就绪";
        DownloadProgress = 0;
        IsDownloading = false;

        LoadDownloadTasks();
    }

    private void LoadDownloadTasks()
    {
        var tasks = _downloadRepository.GetList();
        DownloadTasks.Clear();
        foreach (var task in tasks) DownloadTasks.Add(task);
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl))
        {
            DownloadStatus = "请输入有效的下载链接";
            return;
        }

        if (string.IsNullOrWhiteSpace(DownloadName)) DownloadName = $"下载任务_{DateTime.Now:yyyyMMddHHmmss}";

        IsDownloading = true;
        DownloadStatus = "正在初始化下载...";
        DownloadProgress = 0;

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // 根据URL类型选择适当的下载方式
            if (DownloadUrl.Contains(".m3u8") || DownloadUrl.Contains("playlist"))
                await DownloadWithN_m3u8DL_RE(DownloadUrl, DownloadName, _cancellationTokenSource.Token);
            else
                await PerformHttpDownloadAsync(DownloadUrl, DownloadName, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            DownloadStatus = $"下载错误: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task PerformHttpDownloadAsync(string url, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();

            // 获取远程文件信息
            DownloadStatus = "获取文件信息...";
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            // 确定保存路径
            if (!Directory.Exists(GlobalDefine.DownloadPath))
                Directory.CreateDirectory(GlobalDefine.DownloadPath);
            var downloadPath = Path.Combine(GlobalDefine.DownloadPath, $"{fileName}.mp4");

            using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192,
                true);

            var buffer = new byte[8192];
            var totalBytesRead = 0L;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) != 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;

                if (canReportProgress)
                {
                    var progressPercentage = (double)totalBytesRead / totalBytes * 100;
                    DownloadProgress = progressPercentage;
                    DownloadStatus = $"下载中... {progressPercentage:F1}%";
                }
                else
                {
                    DownloadStatus = $"下载中... {totalBytesRead / 1024 / 1024:F1} MB";
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            // 更新数据库
            var downloadTask = new MediaDownload
            {
                Source = "在线视频",
                Name = fileName,
                Url = url,
                LocalPath = downloadPath,
                IsDownloaded = true,
                DownloadStatus = 2,
                Progress = 100,
                DownloadedBytes = totalBytesRead,
                TotalBytes = totalBytes > 0 ? totalBytes : totalBytesRead,
                SizeText = totalBytes > 0 ? $"{totalBytesRead}/{totalBytes}" : $"已下载 {totalBytesRead}",
                SpeedText = "0 Bps",
                RemainingTimeText = "--:--:--",
                ErrorMessage = null,
                OutputFilePath = downloadPath,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            _downloadRepository.Insert(downloadTask);
            DownloadTasks.Add(downloadTask);

            DownloadStatus = "下载完成";
            DownloadProgress = 100;
        }
        catch (OperationCanceledException)
        {
            DownloadStatus = "下载已取消";
        }
        catch (Exception ex)
        {
            DownloadStatus = $"下载失败: {ex.Message}";
            throw;
        }
    }

    private async Task DownloadWithN_m3u8DL_RE(string url, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            // 确定下载目录
            var downloadDir = GlobalDefine.DownloadPath;

            if (!Directory.Exists(downloadDir))
                Directory.CreateDirectory(downloadDir);

            var outputPath = Path.Combine(downloadDir, fileName);
            var logFilePath = Path.Combine(GlobalDefine.LogsPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{fileName}.log");

            // 构建N_m3u8DL-RE命令行参数
            var arguments = $"\"{url}\" " +
                            $"--save-dir \"{downloadDir}\" " +
                            $"--save-name \"{fileName}\" " +
                            $"--tmp-dir \"{GlobalDefine.TempPath}\" " +
                            $"--log-file-path \"{logFilePath}\" " +
                            $"--ffmpeg-binary-path \"{GlobalDefine.FFmpegPath}\" " +
                            $"--auto-select " +
                            $"--thread-count 16 " +
                            $"--download-retry-count 5 " +
                            $"--mux-after-done";

            // 启动N_m3u8DL-RE进程
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet", // 假设N_m3u8DL-RE作为.NET工具安装
                Arguments =
                    $"\"{AppDomain.CurrentDomain.BaseDirectory}/../N_m3u8DL-RE/src/N_m3u8DL-RE/bin/Debug/net10.0/N_m3u8DL-RE.dll\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _downloadProcess = new Process { StartInfo = processStartInfo };
            _downloadProcess.Start();

            // 异步读取输出
            var outputTask = _downloadProcess.StandardOutput.ReadToEndAsync();
            var errorTask = _downloadProcess.StandardError.ReadToEndAsync();

            // 等待进程完成或取消 - 正确的实现方式
            var processTask = Task.Run(() =>
            {
                _downloadProcess.WaitForExit();
                return _downloadProcess.ExitCode;
            });

            // 创建一个等待取消令牌的任务
            var cancelTask = WaitForCancellationAsync(cancellationToken);

            var completedTask = await Task.WhenAny(processTask, cancelTask);

            if (completedTask == cancelTask)
            {
                // 取消操作 - 尝试终止进程
                try
                {
                    if (!_downloadProcess.HasExited) _downloadProcess.Kill();
                }
                catch
                {
                    // 忽略终止进程时可能出现的异常
                }

                DownloadStatus = "下载已取消";
                return;
            }

            var exitCode = await processTask; // 获取退出码

            if (exitCode == 0)
            {
                // 更新数据库
                var downloadTask = new MediaDownload
                {
                    Source = "在线视频",
                    Name = fileName,
                    Url = url,
                    LocalPath = Path.Combine(downloadDir, $"{fileName}.mp4"), // 默认输出格式
                    IsDownloaded = true,
                    DownloadStatus = 2,
                    Progress = 100,
                    SpeedText = "0 Bps",
                    RemainingTimeText = "--:--:--",
                    ErrorMessage = null,
                    OutputFilePath = Path.Combine(downloadDir, $"{fileName}.mp4"),
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                _downloadRepository.Insert(downloadTask);
                DownloadTasks.Add(downloadTask);

                DownloadStatus = "下载完成";
                DownloadProgress = 100;
            }
            else
            {
                var errorOutput = await errorTask;
                DownloadStatus = $"下载失败，退出码: {exitCode}. 错误: {errorOutput}";
            }
        }
        catch (Exception ex)
        {
            DownloadStatus = $"下载过程中出现错误: {ex.Message}";
            throw;
        }
    }

    // 辅助方法：等待取消令牌
    private async Task WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<object>();
        using (cancellationToken.Register(() => tcs.SetResult(null)))
        {
            await tcs.Task;
        }
    }

    [RelayCommand]
    private void AddToDownloadQueue()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl))
        {
            DownloadStatus = "请输入有效的下载链接";
            return;
        }

        if (string.IsNullOrWhiteSpace(DownloadName)) DownloadName = "新下载任务";

        var downloadTask = new MediaDownload
        {
            Source = "在线视频",
            Name = DownloadName,
            Url = DownloadUrl,
            LocalPath = GlobalDefine.DownloadPath,
            IsDownloaded = false,
            DownloadStatus = 0,
            Progress = 0,
            DownloadedBytes = 0,
            TotalBytes = 0,
            SizeText = "--/--",
            SpeedText = "0 Bps",
            RemainingTimeText = "--:--:--",
            OutputFilePath = null,
            Cover = null,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        _downloadRepository.Insert(downloadTask);
        DownloadTasks.Add(downloadTask);

        DownloadStatus = "已添加到下载队列";
    }

    [RelayCommand]
    private void RemoveDownloadTask(MediaDownload task)
    {
        if (task != null)
        {
            _downloadRepository.Delete(u => u.Id == task.Id);
            DownloadTasks.Remove(task);
        }
    }

    [RelayCommand]
    private void ClearCompletedTasks()
    {
        var completedTasks = DownloadTasks.Where(t => t.IsDownloaded).ToList();
        foreach (var task in completedTasks)
        {
            _downloadRepository.Delete(u => u.Id == task.Id);
            DownloadTasks.Remove(task);
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _cancellationTokenSource?.Cancel();
        if (_downloadProcess != null && !_downloadProcess.HasExited)
            try
            {
                _downloadProcess.Kill();
            }
            catch
            {
                // 忽略终止进程时可能出现的异常
            }

        IsDownloading = false;
        DownloadStatus = "下载已取消";
    }
}