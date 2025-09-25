using System.Text;
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
using N_m3u8DL_RE.Processor;
using N_m3u8DL_RE.Util;

namespace M3U8Download;

public class DownloadManager
{
    public Dictionary<int, DownloadStatus> DownloadStatus { get; private set; } = new Dictionary<int, DownloadStatus>();

    public DownloadOption? Option { get; } = new()
    {
        AdKeywords = [],
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
        MaxSpeed = 1024 * 1024 * 1024, //1G
        UseSystemProxy = false,
        CustomProxy = null, //代理选项
        CustomRange = null, //只下载部分分片

        // 自定义KEY等
        CustomHLSMethod = null,
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
        if (Option == null) return;
        Option.FFmpegBinaryPath = ffmpegPath;
    }

    public async Task<bool> DownloadAsync(string url, string savePath, string saveName)
    {
        if (Option == null) return false;

        Option.Input = url;
        Option.SaveDir = savePath;
        Option.SaveName = saveName;
        HTTPUtil.AppHttpClient.Timeout = TimeSpan.FromSeconds(Option.HttpRequestTimeout);

        Logger.IsWriteFile = !Option.NoLog;
        Logger.LogFilePath = Option.LogFilePath;
        Logger.InitLogFile();
        Logger.LogLevel = Option.LogLevel;

        if (Option.UseSystemProxy == false) HTTPUtil.HttpClientHandler.UseProxy = false;

        if (Option.CustomProxy != null)
        {
            HTTPUtil.HttpClientHandler.Proxy = Option.CustomProxy;
            HTTPUtil.HttpClientHandler.UseProxy = true;
        }

        // 检查互斥的选项
        if (Option is { MuxAfterDone: false, MuxImports.Count: > 0 })
            throw new ArgumentException("MuxAfterDone disabled, MuxImports not allowed!");

        // LivePipeMux开启时 LiveRealTimeMerge必须开启
        if (Option is { LivePipeMux: true, LiveRealTimeMerge: false })
        {
            Logger.WarnMarkUp("LivePipeMux detected, forced enable LiveRealTimeMerge");
            Option.LiveRealTimeMerge = true;
        }

        // 预先检查ffmpeg
        Option.FFmpegBinaryPath ??= GlobalUtil.FindExecutable("ffmpeg");

        if (string.IsNullOrEmpty(Option.FFmpegBinaryPath) || !File.Exists(Option.FFmpegBinaryPath))
            throw new FileNotFoundException(ResString.ffmpegNotFound);

        Logger.Extra($"ffmpeg => {Option.FFmpegBinaryPath}");

        // 预先检查mkvmerge
        if (Option is { MuxOptions.UseMkvmerge: true, MuxAfterDone: true })
        {
            Option.MkvmergeBinaryPath ??= GlobalUtil.FindExecutable("mkvmerge");
            if (string.IsNullOrEmpty(Option.MkvmergeBinaryPath) || !File.Exists(Option.MkvmergeBinaryPath))
                throw new FileNotFoundException(ResString.mkvmergeNotFound);
            Logger.Extra($"mkvmerge => {Option.MkvmergeBinaryPath}");
        }

        // 预先检查
        if (Option.Keys is { Length: > 0 } || Option.KeyTextFile != null)
        {
            if (!string.IsNullOrEmpty(Option.DecryptionBinaryPath) && !File.Exists(Option.DecryptionBinaryPath))
                throw new FileNotFoundException(Option.DecryptionBinaryPath);
            switch (Option.DecryptionEngine)
            {
                case DecryptEngine.SHAKA_PACKAGER:
                {
                    var file = GlobalUtil.FindExecutable("shaka-packager");
                    var file2 = GlobalUtil.FindExecutable("packager-linux-x64");
                    var file3 = GlobalUtil.FindExecutable("packager-osx-x64");
                    var file4 = GlobalUtil.FindExecutable("packager-win-x64");
                    if (file == null && file2 == null && file3 == null && file4 == null)
                        throw new FileNotFoundException(ResString.shakaPackagerNotFound);
                    Option.DecryptionBinaryPath = file ?? file2 ?? file3 ?? file4;
                    Logger.Extra($"shaka-packager => {Option.DecryptionBinaryPath}");
                    break;
                }
                case DecryptEngine.MP4DECRYPT:
                {
                    var file = GlobalUtil.FindExecutable("mp4decrypt");
                    if (file == null) throw new FileNotFoundException(ResString.mp4decryptNotFound);
                    Option.DecryptionBinaryPath = file;
                    Logger.Extra($"mp4decrypt => {Option.DecryptionBinaryPath}");
                    break;
                }
                case DecryptEngine.FFMPEG:
                default:
                    Option.DecryptionBinaryPath = Option.FFmpegBinaryPath;
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
        foreach (var item in Option.Headers)
        {
            headers[item.Key] = item.Value;
            Logger.Extra($"User-Defined Header => {item.Key}: {item.Value}");
        }

        var parserConfig = new ParserConfig
        {
            AppendUrlParams = Option.AppendUrlParams,
            UrlProcessorArgs = Option.UrlProcessorArgs,
            BaseUrl = Option.BaseUrl!,
            Headers = headers,
            CustomMethod = Option.CustomHLSMethod,
            CustomeKey = Option.CustomHLSKey,
            CustomeIV = Option.CustomHLSIv
        };

        if (Option.AllowHlsMultiExtMap) parserConfig.CustomParserArgs.Add("AllowHlsMultiExtMap", "true");

        // demo1
        parserConfig.ContentProcessors.Insert(0, new DemoProcessor());
        // demo2
        parserConfig.KeyProcessors.Insert(0, new DemoProcessor2());
        // for www.nowehoryzonty.pl
        parserConfig.UrlProcessors.Insert(0, new NowehoryzontyUrlProcessor());

        // 等待任务开始时间
        if (Option.TaskStartAt != null && Option.TaskStartAt > DateTime.Now)
        {
            Logger.InfoMarkUp(ResString.taskStartAt + Option.TaskStartAt);
            while (Option.TaskStartAt > DateTime.Now) await Task.Delay(1000);
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
        if (string.IsNullOrEmpty(Option.SaveName)) Option.SaveName = OtherUtil.GetFileNameFromInput(Option.Input);

        // 生成文件夹
        var tmpDir = Path.Combine(Option.TmpDir ?? Environment.CurrentDirectory,
            $"{Option.SaveName ?? DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}");
        // 记录文件
        extractor.RawFiles["meta.json"] = GlobalUtil.ConvertToJson(lists);
        // 写出文件
        await WriteRawFilesAsync(Option, extractor, tmpDir);

        Logger.Info(ResString.streamsInfo, lists.Count, basicStreams.Count, audios.Count, subs.Count);

        foreach (var item in lists) Logger.InfoMarkUp(item.ToString());

        var selectedStreams = new List<StreamSpec>();
        if (Option.DropVideoFilter != null || Option.DropAudioFilter != null || Option.DropSubtitleFilter != null)
        {
            basicStreams = FilterUtil.DoFilterDrop(basicStreams, Option.DropVideoFilter);
            audios = FilterUtil.DoFilterDrop(audios, Option.DropAudioFilter);
            subs = FilterUtil.DoFilterDrop(subs, Option.DropSubtitleFilter);
            lists = basicStreams.Concat(audios).Concat(subs).ToList();
        }

        if (Option.DropVideoFilter != null) Logger.Extra($"DropVideoFilter => {Option.DropVideoFilter}");
        if (Option.DropAudioFilter != null) Logger.Extra($"DropAudioFilter => {Option.DropAudioFilter}");
        if (Option.DropSubtitleFilter != null) Logger.Extra($"DropSubtitleFilter => {Option.DropSubtitleFilter}");
        if (Option.VideoFilter != null) Logger.Extra($"VideoFilter => {Option.VideoFilter}");
        if (Option.AudioFilter != null) Logger.Extra($"AudioFilter => {Option.AudioFilter}");
        if (Option.SubtitleFilter != null) Logger.Extra($"SubtitleFilter => {Option.SubtitleFilter}");

        if (Option.AutoSelect)
        {
            if (basicStreams.Count != 0)
                selectedStreams.Add(basicStreams.First());
            var langs = audios.DistinctBy(a => a.Language).Select(a => a.Language);
            foreach (var lang in langs)
                selectedStreams.Add(audios.Where(a => a.Language == lang).OrderByDescending(a => a.Bandwidth)
                    .ThenByDescending(GetOrder).First());
            selectedStreams.AddRange(subs);
        }
        else if (Option.SubOnly)
        {
            selectedStreams.AddRange(subs);
        }
        else if (Option.VideoFilter != null || Option.AudioFilter != null || Option.SubtitleFilter != null)
        {
            basicStreams = FilterUtil.DoFilterKeep(basicStreams, Option.VideoFilter);
            audios = FilterUtil.DoFilterKeep(audios, Option.AudioFilter);
            subs = FilterUtil.DoFilterKeep(subs, Option.SubtitleFilter);
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
        var livingFlag = selectedStreams.Any(s => s.Playlist?.IsLive == true) && !Option.LivePerformAsVod;
        if (livingFlag) Logger.WarnMarkUp($"[white on darkorange3_1]{ResString.liveFound}[/]");

        // 无法识别的加密方式，自动开启二进制合并
        if (selectedStreams.Any(s =>
                s.Playlist!.MediaParts.Any(p =>
                    p.MediaSegments.Any(m => m.EncryptInfo.Method == EncryptMethod.UNKNOWN))))
        {
            Logger.WarnMarkUp($"[darkorange3_1]{ResString.autoBinaryMerge3}[/]");
            Option.BinaryMerge = true;
        }

        // 应用用户自定义的分片范围
        if (!livingFlag)
            FilterUtil.ApplyCustomRange(selectedStreams, Option.CustomRange);

        // 应用用户自定义的广告分片关键字
        FilterUtil.CleanAd(selectedStreams, Option.AdKeywords);

        // 记录文件
        extractor.RawFiles["meta_selected.json"] = GlobalUtil.ConvertToJson(selectedStreams);

        Logger.Info(ResString.selectedStream);
        foreach (var item in selectedStreams) Logger.InfoMarkUp(item.ToString());

        // 写出文件
        await WriteRawFilesAsync(Option, extractor, tmpDir);

        if (Option.SkipDownload) return false;


        // 开始MuxAfterDone后自动使用二进制版
        if (Option is { BinaryMerge: false, MuxAfterDone: true })
        {
            Option.BinaryMerge = true;
            Logger.WarnMarkUp($"[darkorange3_1]{ResString.autoBinaryMerge6}[/]");
        }

        // 下载配置
        var downloadConfig = new DownloaderConfig
        {
            MyOptions = Option,
            DirPrefix = tmpDir,
            Headers = parserConfig.Headers // 使用命令行解析得到的Headers
        };

        bool result;

        if (extractor.ExtractorType == ExtractorType.HTTP_LIVE)
        {
            var sldm = new HTTPLiveRecordManager(downloadConfig, selectedStreams, extractor);
            result = await sldm.StartRecordAsync();
        }
        else if (!livingFlag)
        {
            // 开始下载
            var sdm = new LunaDownloadManager(downloadConfig, selectedStreams, extractor, DownloadStatus);
            result = await sdm.StartDownloadAsync();
        }
        else
        {
            var sldm = new SimpleLiveRecordManager2(downloadConfig, selectedStreams, extractor);
            result = await sldm.StartRecordAsync();
        }

        return result;
    }

    private static async Task WriteRawFilesAsync(DownloadOption option, StreamExtractor extractor, string tmpDir)
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