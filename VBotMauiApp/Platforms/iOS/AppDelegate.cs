using System;
using System.Linq;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using UIKit;
using VBot.iOS.SDK;
using VBotMauiApp.Services;

namespace VBotMauiApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public static string CachedPushKitToken { get; set; } = string.Empty;
    public static Action<string>? OnVoipTokenReceived;

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        try
        {
            AppLogger.Log("VBotSDK", "Khởi tạo VBot SDK trong AppDelegate (qua Swift Wrapper)...");

            // Khởi tạo VBot SDK và PushKit qua Native Wrapper
            VBotWrapper.Shared.Initialize(
                environment: AppConfig.Environment,
                customBaseUrl: AppConfig.CustomBaseUrl
            );

            AppLogger.Log("VBotSDK", "Đã khởi tạo VBotWrapper.Shared.Initialize thành công.");

            // Lắng nghe PushKit token từ Swift wrapper
            VBotWrapper.Shared.Delegate = new WrapperPushKitDelegate();

            // Lắng nghe toàn bộ log chi tiết từ SDK
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("VBotInternalLogNotification"), notification =>
            {
                try
                {
                    if (notification.UserInfo != null)
                    {
                        var tag = notification.UserInfo[new NSString("tag")]?.ToString() ?? "SDK";
                        var msg = notification.UserInfo[new NSString("message")]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(msg))
                        {
                            AppLogger.Log(tag, msg);
                        }
                    }
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            AppLogger.Log("VBotSDK", $"Lỗi khởi tạo VBotWrapper: {ex.Message}");
        }

        return base.FinishedLaunching(application, launchOptions);
    }
}

/// <summary>
/// Delegate nhận callback và PushKit token từ Swift wrapper
/// </summary>
public class WrapperPushKitDelegate : VBotWrapperDelegate
{
    public override void OnPushKitTokenReceived(string token)
    {
        AppLogger.Log("PushKit", $"[Swift Wrapper] Nhận được VoIP Token: {token}");
        AppDelegate.CachedPushKitToken = token;
        AppDelegate.OnVoipTokenReceived?.Invoke(token);
    }

    public override void OnCallStateChanged(string state, string name, bool isIncoming, bool isMute, bool onHold)
    {
        AppLogger.Log("VBotSDK", $"[Swift Wrapper] CallState: {state}, Name: {name}, Incoming: {isIncoming}");
        VBotPhoneService.Instance?.OnWrapperCallStateChanged(state, name, isIncoming, isMute, onHold);
    }

    public override void OnCallEnded(string reason, string endedBy)
    {
        AppLogger.Log("VBotSDK", $"[Swift Wrapper] CallEnded: reason={reason}, endedBy={endedBy}");
        VBotPhoneService.Instance?.OnWrapperCallEnded(reason, endedBy);
    }

    public override void OnCallMuteStateDidChange(bool muted)
    {
        AppLogger.Log("VBotSDK", $"[Swift Wrapper] MuteChanged: {muted}");
        if (VBotPhoneService.Instance?.CurrentCallState != null)
        {
            VBotPhoneService.Instance.MuteAsync();
        }
    }

    public override void OnCallStarted()
    {
        AppLogger.Log("VBotSDK", "[Swift Wrapper] CallStarted");
    }

    public override void OnCallAccepted()
    {
        AppLogger.Log("VBotSDK", "[Swift Wrapper] CallAccepted");
    }
}
