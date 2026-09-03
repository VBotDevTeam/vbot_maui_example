using Android.Content;
using VBotMauiApp.Services;

namespace VBotMauiApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class HangupReceiver : BroadcastReceiver
{
    public override async void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        OngoingCallNotification.Cancel(context);

        var phoneService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(IVBotPhoneService)) as IVBotPhoneService;
        if (phoneService != null)
        {
            await phoneService.HangupAsync();
        }
    }
}
