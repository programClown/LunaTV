using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace LunaTV.Utils;

public class DownloadHLS
{
    public static async Task Download(string url, string outputPath)
    {
        try
        {
            using (var libvlc = new LibVLC())
            using (var mediaPlayer = new MediaPlayer(libvlc))
            {
                mediaPlayer.EndReached += (sender, e) => { Console.WriteLine("下载完成 " + outputPath); };
                mediaPlayer.EncounteredError += (s, e) => { Console.WriteLine("下载失败"); };

                // 设置转码选项 - 将流保存为 MP4 文件
                // 这里使用 :sout=#duplicate{dst=std{access=file,mux=mp4,dst='output.mp4'}}
                var soutOptions = $"#duplicate{{dst=std{{access=file,mux=mp4,dst='{outputPath}'}}}}";


                // Create new media with HLS link
                using (var media = new Media(libvlc, new Uri(url),
                           // Define stream output options.
                           // In this case stream to a file with the given path and play locally the stream while streaming it.
                           $"sout={soutOptions}",
                           ":sout-keep"))
                {
                    // Start recording
                    // 开始播放（实际上会开始下载和转码）
                    await Task.Run(() => mediaPlayer.Play(media));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void Download(List<string> urls, List<string> outputPaths)
    {
    }
}