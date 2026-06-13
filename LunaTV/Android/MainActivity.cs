using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Avalonia.Android;

namespace LunaTV.Android;

[Activity(
    Name = "com.lunatv.app.MainActivity",
    Theme = "@style/MyTheme.NoActionBar",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleInstance,
    ConfigurationChanges = ConfigChanges.Orientation
                           | ConfigChanges.ScreenSize
                           | ConfigChanges.UiMode
                           | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize
                           | ConfigChanges.Density)]
public sealed class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            base.OnCreate(savedInstanceState);
        }
        catch (System.Exception ex)
        {
            Log.Error("LunaTV", $"OnCreate CRASHED: {ex}");
            throw;
        }
    }
}
