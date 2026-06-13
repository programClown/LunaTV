using System.IO;
using System.Text.Json;
using LunaTV.Constants;

namespace LunaTV.Services;

public class AppJsonConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public T? ReadJson<T>()
    {
        return ReadJson<T>(GlobalDefine.AppJsonPath);
    }

    public void WriteJson<T>(T data)
    {
        WriteJson(GlobalDefine.AppJsonPath, data);
    }

    public static T? ReadJson<T>(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // 使用FileShare.Read允许其他进程读取但不允许写入
        using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read,
                   FileShare.Read))
        using (var reader = new StreamReader(stream))
        {
            string json = reader.ReadToEnd();
            if (string.IsNullOrEmpty(json))
                return default(T);

            return JsonSerializer.Deserialize<T>(json);
        }
    }

    public static void WriteJson<T>(string path, T data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // 使用FileShare.None禁止其他进程访问文件
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            writer.Write(json);
        }
    }
}