using System.IO;
using System.Text.Json;
using LunaTV.Constants;
using LunaTV.Models;

namespace LunaTV.Services;

public class AppJsonConfigService
{
    public T ReadJson<T>()
    {
        // 使用FileShare.Read允许其他进程读取但不允许写入
        using (var stream = new FileStream(GlobalDefine.AppJsonPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
        using (var reader = new StreamReader(stream))
        {
            string json = reader.ReadToEnd();
            if (string.IsNullOrEmpty(json))
                return default(T);
                
            return JsonSerializer.Deserialize<T>(json);
        }
    }

    public void WriteJson<T>(T data)
    {
        // 使用FileShare.None禁止其他进程访问文件
        using (var stream = new FileStream(GlobalDefine.AppJsonPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            writer.Write(json);
        }
    }
}