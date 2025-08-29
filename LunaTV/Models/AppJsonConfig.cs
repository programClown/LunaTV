namespace LunaTV.Models;

public class AppJsonConfig
{
    public string Version { get; set; } = "1.0.0";
    public Player Player { get; set; }
}

public class Player
{
    public float Vol { get; set; } = 50; //音量
    public bool Muted { get; set; } //
}