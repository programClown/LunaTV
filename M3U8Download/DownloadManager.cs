using System.Net;
using System.Text;
using N_m3u8DL_RE.CommandLine;
using N_m3u8DL_RE.Common.Entity;
using N_m3u8DL_RE.Common.Enum;
using N_m3u8DL_RE.Common.Log;
using N_m3u8DL_RE.Common.Resource;
using N_m3u8DL_RE.Common.Util;
using N_m3u8DL_RE.Config;
using N_m3u8DL_RE.DownloadManager;
using N_m3u8DL_RE.Enum;
using N_m3u8DL_RE.Parser;
using N_m3u8DL_RE.Parser.Config;
using N_m3u8DL_RE.Util;

namespace M3U8Download;

public class DownloadManager
{
    private readonly MyOption? _option = new()
    {
        SavePattern = "<SaveName>_<Id>_<Codecs>_<Language>_<Ext>",
        TmpDir = Path.Combine(Environment.CurrentDirectory, "tmp"),
        UILanguage = "zh-CN",
        LogLevel = LogLevel.INFO,
        SubtitleFormat = SubtitleFormat.SRT,
        DisableUpdateCheck = true,
        AutoSelect = false,
        SubOnly = false,
        ThreadCount = Environment.ProcessorCount,
        DownloadRetryCount = 3,
        HttpRequestTimeout = 100,
        SkipMerge = false,
        SkipDownload = false,
        NoDateInfo = false,
        BinaryMerge = false,
        UseFFmpegConcatDemuxer = false,
        DelAfterDone = true,
        AutoSubtitleFix = true,
        CheckSegmentsCount = true,
        WriteMetaJson = true,
        AppendUrlParams = false,
        MP4RealTimeDecryption = false,
        DecryptionEngine = DecryptEngine.MP4DECRYPT,
        DecryptionBinaryPath = null,
        FFmpegBinaryPath = null,
        BaseUrl = null,
        ConcurrentDownload = false,
        NoLog = false,
        AllowHlsMultiExtMap = false,
        MaxSpeed = long.MaxValue,
        UseSystemProxy = false,
        CustomProxy = new WebProxy(), //代理选项
        CustomRange = null, //只下载部分分片

        // 自定义KEY等
        CustomHLSMethod = EncryptMethod.NONE,
        CustomHLSKey = null,
        CustomHLSIv = null,

        // 任务开始时间
        TaskStartAt = DateTime.Now,

        // 直播相关
        LivePerformAsVod = false,
        LiveRealTimeMerge = false,
        LiveKeepSegments = true,
        LivePipeMux = false,
        LiveRecordLimit = null,
        LiveWaitTime = 0,
        LiveTakeCount = 16,
        LiveFixVttByAudio = false,

        // 复杂命令行如下
        MuxAfterDone = false,
        MuxOptions = null,
        MuxImports = null,
        VideoFilter = null,
        AudioFilter = null,
        SubtitleFilter = null,
        DropVideoFilter = null,
        DropAudioFilter = null,
        DropSubtitleFilter = null
    };

    private static int GetOrder(StreamSpec streamSpec)
    {
        if (streamSpec.Channels == null) return 0;

        var str = streamSpec.Channels.Split('/')[0];
        return int.TryParse(str, out var order) ? order : 0;
    }

    public void SetFFmpegPath(string ffmpegPath)
    {
        if (_option == null) return;
        _option.FFmpegBinaryPath = ffmpegPath;
    }

    public async Task<bool> DownloadAsync(string url, string savePath, string saveName)
    {
        if (_option == null) return false;

        _option.Input = url;
        _option.SaveDir = savePath;
        _option.SaveName = saveName;
        HTTPUtil.AppHttpClient.Timeout = TimeSpan.FromSeconds(_option.HttpRequestTimeout);

        Logger.IsWriteFile = !_option.NoLog;
        Logger.LogFilePath = _option.LogFilePath;
        Logger.InitLogFile();
        Logger.LogLevel = _option.LogLevel;

        if (_option.UseSystemProxy == false) HTTPUtil.HttpClientHandler.UseProxy = false;

        if (_option.CustomProxy != null)
        {
            HTTPUtil.HttpClientHandler.Proxy = _option.CustomProxy;
            HTTPUtil.HttpClientHandler.UseProxy = true;
        }

        // 检查互斥的选项
        if (_option is { MuxAfterDone: false, MuxImports.Count: > 0 })
            throw new ArgumentException("MuxAfterDone disabled, MuxImports not allowed!");

        // LivePipeMux开启时 LiveRealTimeMerge必须开启
        if (_option is { LivePipeMux: true, LiveRealTimeMerge: false })
        {
            Logger.WarnMarkUp("LivePipeMux detected, forced enable LiveRealTimeMerge");
            _option.LiveRealTimeMerge = true;
        }

        // 预先检查ffmpeg
        _option.FFmpegBinaryPath ??= GlobalUtil.FindExecutable("ffmpeg");

        if (string.IsNullOrEmpty(_option.FFmpegBinaryPath) || !File.Exists(_option.FFmpegBinaryPath))
            throw new FileNotFoundException(ResString.ffmpegNotFound);

        Logger.Extra($"ffmpeg => {_option.FFmpegBinaryPath}");

        // 预先检查mkvmerge
        if (_option is { MuxOptions.UseMkvmerge: true, MuxAfterDone: true })
        {
            _option.MkvmergeBinaryPath ??= GlobalUtil.FindExecutable("mkvmerge");
            if (string.IsNullOrEmpty(_option.MkvmergeBinaryPath) || !File.Exists(_option.MkvmergeBinaryPath))
                throw new FileNotFoundException(ResString.mkvmergeNotFound);
            Logger.Extra($"mkvmerge => {_option.MkvmergeBinaryPath}");
        }

        // 预先检查
        if (_option.Keys is { Length: > 0 } || _option.KeyTextFile != null)
        {
            if (!string.IsNullOrEmpty(_option.DecryptionBinaryPath) && !File.Exists(_option.DecryptionBinaryPath))
                throw new FileNotFoundException(_option.DecryptionBinaryPath);
            switch (_option.DecryptionEngine)
            {
                case DecryptEngine.SHAKA_PACKAGER:
                {
                    var file = GlobalUtil.FindExecutable("shaka-packager");
                    var file2 = GlobalUtil.FindExecutable("packager-linux-x64");
                    var file3 = GlobalUtil.FindExecutable("packager-osx-x64");
                    var file4 = GlobalUtil.FindExecutable("packager-win-x64");
                    if (file == null && file2 == null && file3 == null && file4 == null)
                        throw new FileNotFoundException(ResString.shakaPackagerNotFound);
                    _option.DecryptionBinaryPath = file ?? file2 ?? file3 ?? file4;
                    Logger.Extra($"shaka-packager => {_option.DecryptionBinaryPath}");
                    break;
                }
                case DecryptEngine.MP4DECRYPT:
                {
                    var file = GlobalUtil.FindExecutable("mp4decrypt");
                    if (file == null) throw new FileNotFoundException(ResString.mp4decryptNotFound);
                    _option.DecryptionBinaryPath = file;
                    Logger.Extra($"mp4decrypt => {_option.DecryptionBinaryPath}");
                    break;
                }
                case DecryptEngine.FFMPEG:
                default:
                    _option.DecryptionBinaryPath = _option.FFmpegBinaryPath;
                    break;
            }
        }

        // 默认的headers
        var headers = new Dictionary<string, string>
        {
            ["user-agent"] =
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36"
        };

        // 添加或替换用户输入的headers
        foreach (var item in _option.Headers)
        {
            headers[item.Key] = item.Value;
            Logger.Extra($"User-Defined Header => {item.Key}: {item.Value}");
        }

        var parserConfig = new ParserConfig
        {
            AppendUrlParams = _option.AppendUrlParams,
            UrlProcessorArgs = _option.UrlProcessorArgs,
            BaseUrl = _option.BaseUrl!,
            Headers = headers,
            CustomMethod = _option.CustomHLSMethod,
            CustomeKey = _option.CustomHLSKey,
            CustomeIV = _option.CustomHLSIv
        };

        if (_option.AllowHlsMultiExtMap) parserConfig.CustomParserArgs.Add("AllowHlsMultiExtMap", "true");

        // 等待任务开始时间
        if (_option.TaskStartAt != null && _option.TaskStartAt > DateTime.Now)
        {
            Logger.InfoMarkUp(ResString.taskStartAt + _option.TaskStartAt);
            while (_option.TaskStartAt > DateTime.Now) await Task.Delay(1000);
        }

        // 流提取器配置
        var extractor = new StreamExtractor(parserConfig);
        // 从链接加载内容
        await RetryUtil.WebRequestRetryAsync(async () =>
        {
            await extractor.LoadSourceFromUrlAsync(url);
            return true;
        });
        // 解析流信息
        var streams = await extractor.ExtractStreamsAsync();


        // 全部媒体
        var lists = streams.OrderBy(p => p.MediaType).ThenByDescending(p => p.Bandwidth).ThenByDescending(GetOrder)
            .ToList();
        // 基本流
        var basicStreams = lists.Where(x => x.MediaType is null or MediaType.VIDEO).ToList();
        // 可选音频轨道
        var audios = lists.Where(x => x.MediaType == MediaType.AUDIO).ToList();
        // 可选字幕轨道
        var subs = lists.Where(x => x.MediaType == MediaType.SUBTITLES).ToList();

        // 尝试从URL或文件读取文件名
        if (string.IsNullOrEmpty(_option.SaveName)) _option.SaveName = OtherUtil.GetFileNameFromInput(_option.Input);

        // 生成文件夹
        var tmpDir = Path.Combine(_option.TmpDir ?? Environment.CurrentDirectory,
            $"{_option.SaveName ?? DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}");
        // 记录文件
        extractor.RawFiles["meta.json"] = GlobalUtil.ConvertToJson(lists);
        // 写出文件
        await WriteRawFilesAsync(_option, extractor, tmpDir);

        Logger.Info(ResString.streamsInfo, lists.Count, basicStreams.Count, audios.Count, subs.Count);

        foreach (var item in lists) Logger.InfoMarkUp(item.ToString());

        var selectedStreams = new List<StreamSpec>();
        if (_option.DropVideoFilter != null || _option.DropAudioFilter != null || _option.DropSubtitleFilter != null)
        {
            basicStreams = FilterUtil.DoFilterDrop(basicStreams, _option.DropVideoFilter);
            audios = FilterUtil.DoFilterDrop(audios, _option.DropAudioFilter);
            subs = FilterUtil.DoFilterDrop(subs, _option.DropSubtitleFilter);
            lists = basicStreams.Concat(audios).Concat(subs).ToList();
        }

        if (_option.DropVideoFilter != null) Logger.Extra($"DropVideoFilter => {_option.DropVideoFilter}");
        if (_option.DropAudioFilter != null) Logger.Extra($"DropAudioFilter => {_option.DropAudioFilter}");
        if (_option.DropSubtitleFilter != null) Logger.Extra($"DropSubtitleFilter => {_option.DropSubtitleFilter}");
        if (_option.VideoFilter != null) Logger.Extra($"VideoFilter => {_option.VideoFilter}");
        if (_option.AudioFilter != null) Logger.Extra($"AudioFilter => {_option.AudioFilter}");
        if (_option.SubtitleFilter != null) Logger.Extra($"SubtitleFilter => {_option.SubtitleFilter}");

        if (_option.AutoSelect)
        {
            if (basicStreams.Count != 0)
                selectedStreams.Add(basicStreams.First());
            var langs = audios.DistinctBy(a => a.Language).Select(a => a.Language);
            foreach (var lang in langs)
                selectedStreams.Add(audios.Where(a => a.Language == lang).OrderByDescending(a => a.Bandwidth)
                    .ThenByDescending(GetOrder).First());
            selectedStreams.AddRange(subs);
        }
        else if (_option.SubOnly)
        {
            selectedStreams.AddRange(subs);
        }
        else if (_option.VideoFilter != null || _option.AudioFilter != null || _option.SubtitleFilter != null)
        {
            basicStreams = FilterUtil.DoFilterKeep(basicStreams, _option.VideoFilter);
            audios = FilterUtil.DoFilterKeep(audios, _option.AudioFilter);
            subs = FilterUtil.DoFilterKeep(subs, _option.SubtitleFilter);
            selectedStreams = basicStreams.Concat(audios).Concat(subs).ToList();
        }
        else
        {
            // 展示交互式选择框
            selectedStreams = FilterUtil.SelectStreams(lists);
        }

        if (selectedStreams.Count == 0)
            throw new Exception(ResString.noStreamsToDownload);

        // HLS: 选中流中若有没加载出playlist的，加载playlist
        // DASH/MSS: 加载playlist (调用url预处理器)
        if (selectedStreams.Any(s => s.Playlist == null) || extractor.ExtractorType == ExtractorType.MPEG_DASH ||
            extractor.ExtractorType == ExtractorType.MSS)
            await extractor.FetchPlayListAsync(selectedStreams);

        // 直播检测
        var livingFlag = selectedStreams.Any(s => s.Playlist?.IsLive == true) && !_option.LivePerformAsVod;
        if (livingFlag) Logger.WarnMarkUp($"[white on darkorange3_1]{ResString.liveFound}[/]");

        // 无法识别的加密方式，自动开启二进制合并
        if (selectedStreams.Any(s =>
                s.Playlist!.MediaParts.Any(p =>
                    p.MediaSegments.Any(m => m.EncryptInfo.Method == EncryptMethod.UNKNOWN))))
        {
            Logger.WarnMarkUp($"[darkorange3_1]{ResString.autoBinaryMerge3}[/]");
            _option.BinaryMerge = true;
        }

        // 应用用户自定义的分片范围
        if (!livingFlag)
            FilterUtil.ApplyCustomRange(selectedStreams, _option.CustomRange);

        // 应用用户自定义的广告分片关键字
        FilterUtil.CleanAd(selectedStreams, _option.AdKeywords);

        // 记录文件
        extractor.RawFiles["meta_selected.json"] = GlobalUtil.ConvertToJson(selectedStreams);

        Logger.Info(ResString.selectedStream);
        foreach (var item in selectedStreams) Logger.InfoMarkUp(item.ToString());

        // 写出文件
        await WriteRawFilesAsync(_option, extractor, tmpDir);

        if (_option.SkipDownload) return false;


        // 开始MuxAfterDone后自动使用二进制版
        if (_option is { BinaryMerge: false, MuxAfterDone: true })
        {
            _option.BinaryMerge = true;
            Logger.WarnMarkUp($"[darkorange3_1]{ResString.autoBinaryMerge6}[/]");
        }

        // 下载配置
        var downloadConfig = new DownloaderConfig
        {
            MyOptions = _option,
            DirPrefix = tmpDir,
            Headers = parserConfig.Headers // 使用命令行解析得到的Headers
        };

        var result = false;

        if (extractor.ExtractorType == ExtractorType.HTTP_LIVE)
        {
            var sldm = new HTTPLiveRecordManager(downloadConfig, selectedStreams, extractor);
            result = await sldm.StartRecordAsync();
        }
        else if (!livingFlag)
        {
            // 开始下载
            var sdm = new SimpleDownloadManager(downloadConfig, selectedStreams, extractor);
            result = await sdm.StartDownloadAsync();
        }
        else
        {
            var sldm = new SimpleLiveRecordManager2(downloadConfig, selectedStreams, extractor);
            result = await sldm.StartRecordAsync();
        }

        return result;
    }

    private static async Task WriteRawFilesAsync(MyOption option, StreamExtractor extractor, string tmpDir)
    {
        // 写出json文件
        if (option.WriteMetaJson)
        {
            if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
            Logger.Warn(ResString.writeJson);
            foreach (var item in extractor.RawFiles)
            {
                var file = Path.Combine(tmpDir, item.Key);
                if (!File.Exists(file)) await File.WriteAllTextAsync(file, item.Value, Encoding.UTF8);
            }
        }
    }
}