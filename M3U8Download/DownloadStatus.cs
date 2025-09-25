namespace M3U8Download;

public class DownloadStatus
{
    public double percentage { get; set; }
    public long size { get; set; }
    public long totalSize { get; set; }
    public string sizeStr { get; set; } = String.Empty;
    public string speed { get; set; } = String.Empty;
    public TimeSpan? remainingTime { get; set; }
    public string remainingTimeStr { get; set; } = string.Empty;
    public string? description { get; set; }
}