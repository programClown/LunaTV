using Android.App;
using Android.Runtime;
using Android.Util;
using Avalonia;
using Avalonia.Android;
using LunaTV.Constants;

namespace LunaTV.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<App>
{
    public MainApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        try
        {
            Log.Info("LunaTV", $"DataPath: {GlobalDefine.DataPath}");
            Log.Info("LunaTV", $"DbConn: {GlobalDefine.DbConn}");
            var result = Program.ConfigureSharedAppBuilder(base.CustomizeAppBuilder(builder));
            Log.Info("LunaTV", "ConfigureSharedAppBuilder completed OK");

            // Log DB init error if any
            var dbErr = LunaTV.Base.DB.SqlSugarServiceExtensions.LastInitError;
            if (dbErr != null) Log.Error("LunaTV", $"DB INIT ERROR: {dbErr}");

            return result;
        }
        catch (System.Exception ex)
        {
            Log.Error("LunaTV", $"CustomizeAppBuilder CRASHED: {ex}");
            throw;
        }
    }
}
