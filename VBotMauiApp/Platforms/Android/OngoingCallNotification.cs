using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace VBotMauiApp.Platforms.Android;

public static class OngoingCallNotification
{
    private const string ChannelId = "vbot_ongoing_call_channel";
    private const int NotificationId = 9999;

    public static void Show(Context context, string callerName)
    {
        var appContext = context.ApplicationContext ?? context;
        var notificationManager = appContext.GetSystemService(Context.NotificationService) as NotificationManager;
        if (notificationManager == null) return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                "Cuộc gọi đang diễn ra",
                NotificationImportance.Low
            )
            {
                Description = "Hiển thị thông báo khi đang trong cuộc gọi VBot"
            };
            channel.SetShowBadge(false);
            channel.SetSound(null, null);
            channel.EnableVibration(false);
            notificationManager.CreateNotificationChannel(channel);
        }

        var openAppIntent = new Intent(appContext, typeof(MainActivity));
        openAppIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.NewTask);
        var contentPendingIntent = PendingIntent.GetActivity(
            appContext, 0, openAppIntent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
        );

        var hangupIntent = new Intent(appContext, typeof(HangupReceiver));
        var hangupPendingIntent = PendingIntent.GetBroadcast(
            appContext, 1, hangupIntent,
            PendingIntentFlags.Immutable
        );

        var builder = new NotificationCompat.Builder(appContext, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCall)
            .SetContentTitle("Đang trong cuộc gọi")
            .SetContentText(string.IsNullOrEmpty(callerName) ? "VBot Phone" : callerName)
            .SetOngoing(true)
            .SetCategory(NotificationCompat.CategoryCall)
            .SetContentIntent(contentPendingIntent)
            .AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "Tắt máy", hangupPendingIntent)
            .SetPriority(NotificationCompat.PriorityLow)
            .SetVisibility(NotificationCompat.VisibilityPublic);

        notificationManager.Notify(NotificationId, builder.Build());
    }

    public static void Cancel(Context context)
    {
        var appContext = context.ApplicationContext ?? context;
        var notificationManager = appContext.GetSystemService(Context.NotificationService) as NotificationManager;
        notificationManager?.Cancel(NotificationId);
    }
}
