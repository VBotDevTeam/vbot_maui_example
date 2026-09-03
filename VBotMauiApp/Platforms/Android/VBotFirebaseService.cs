using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Firebase.Messaging;
using VBotMauiApp.Services;

namespace VBotMauiApp.Platforms.Android;

/// <summary>
/// Service tiếp nhận FCM Push Notification khi có cuộc gọi đến
/// </summary>
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class VBotFirebaseService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        var data = message.Data;

        if (data != null && data.ContainsKey("transId"))
        {
            var map = new Dictionary<string, string>();
            foreach (var kv in data)
            {
                map[kv.Key] = kv.Value;
            }

            VBotPhoneService.HandleIncomingPushNotification(map);
        }
    }
}


