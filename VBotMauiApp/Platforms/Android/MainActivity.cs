using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui;

namespace VBotMauiApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var policy = new Android.OS.StrictMode.ThreadPolicy.Builder().PermitAll().Build();
        Android.OS.StrictMode.SetThreadPolicy(policy);
        RequestVoIPPermissions();
    }

    private void RequestVoIPPermissions()
    {
        var neededPermissions = new System.Collections.Generic.List<string>
        {
            Manifest.Permission.RecordAudio,
            Manifest.Permission.ModifyAudioSettings
        };

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            neededPermissions.Add(Manifest.Permission.PostNotifications);
        }

        var ungranted = new System.Collections.Generic.List<string>();
        foreach (var perm in neededPermissions)
        {
            if (ContextCompat.CheckSelfPermission(this, perm) != Permission.Granted)
            {
                ungranted.Add(perm);
            }
        }

        if (ungranted.Count > 0)
        {
            ActivityCompat.RequestPermissions(this, ungranted.ToArray(), 1001);
        }

        // Overlay permission (SYSTEM_ALERT_WINDOW)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            if (!Settings.CanDrawOverlays(this))
            {
                var intent = new Intent(
                    Settings.ActionManageOverlayPermission,
                    global::Android.Net.Uri.Parse($"package:{PackageName}")
                );
                StartActivity(intent);
            }
        }

        // Full Screen Intent trên Android 14+ (UpsideDownCake)
        // Cần thiết để hiển thị incoming call notification dạng full-screen
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
        {
            try
            {
                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                if (notificationManager != null && !notificationManager.CanUseFullScreenIntent())
                {
                    var intent = new Intent(
                        Settings.ActionManageAppUseFullScreenIntent,
                        global::Android.Net.Uri.Parse($"package:{PackageName}")
                    );
                    StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Warn("VBotPhone", $"Could not open FullScreenIntent settings: {ex.Message}");
            }
        }
    }
}
