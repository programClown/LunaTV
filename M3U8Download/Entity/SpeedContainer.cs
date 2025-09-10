﻿namespace N_m3u8DL_RE.Entity;

internal class SpeedContainer
{
    public bool SingleSegment { get; set; } = false;
    public long NowSpeed { get; set; } = 0L; // 当前每秒速度
    public long SpeedLimit { get; set; } = long.MaxValue; // 限速设置
    public long? ResponseLength { get; set; }
    public long RDownloaded => _Rdownloaded;
    private int _zeroSpeedCount = 0;
    public int LowSpeedCount => _zeroSpeedCount;
    public bool ShouldStop => LowSpeedCount >= 20;

    ///////////////////////////////////////////////////

    private long _downloaded = 0;
    private long _Rdownloaded = 0;
    public long Downloaded => _downloaded;

    public int AddLowSpeedCount()
    {
        return Interlocked.Add(ref _zeroSpeedCount, 1);
    }

    public int ResetLowSpeedCount()
    {
        return Interlocked.Exchange(ref _zeroSpeedCount, 0);
    }

    public long Add(long size)
    {
        Interlocked.Add(ref _Rdownloaded, size);
        return Interlocked.Add(ref _downloaded, size);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _downloaded, 0);
    }

    public void ResetVars()
    {
        Reset();
        ResetLowSpeedCount();
        SingleSegment = false;
        ResponseLength = null;
        _Rdownloaded = 0L;
    }
}