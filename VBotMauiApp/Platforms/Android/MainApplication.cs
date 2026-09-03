using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace VBotMauiApp;

public class VBotUncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
{
    private readonly Java.Lang.Thread.IUncaughtExceptionHandler? _defaultHandler;

    public VBotUncaughtExceptionHandler(Java.Lang.Thread.IUncaughtExceptionHandler? defaultHandler)
    {
        _defaultHandler = defaultHandler;
    }

    public void UncaughtException(Java.Lang.Thread t, Java.Lang.Throwable e)
    {
        var msg = e.Message ?? string.Empty;
        var fullString = e.ToString() ?? string.Empty;
        if (fullString.Contains("70013") || fullString.Contains("pjsua_call_answer2") || fullString.Contains("PJ_EINVALIDOP"))
        {
            global::Android.Util.Log.Warn("VBotPhone", $"Handled uncaught native SDK PJSUA duplicate answer exception: {msg}");
            return;
        }

        _defaultHandler?.UncaughtException(t, e);
    }
}

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // Bắt và bỏ qua các exception lặp Answer từ native SDK (PJ_EINVALIDOP / 70013)
        var defaultHandler = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
        Java.Lang.Thread.DefaultUncaughtExceptionHandler = new VBotUncaughtExceptionHandler(defaultHandler);

        AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
        {
            var msg = args.Exception.ToString();
            if (msg.Contains("70013") || msg.Contains("pjsua_call_answer2") || msg.Contains("PJ_EINVALIDOP"))
            {
                global::Android.Util.Log.Warn("VBotPhone", $"AndroidEnvironment caught PJSUA 70013 exception: {args.Exception.Message}");
                args.Handled = true;
            }
        };

        var policy = new Android.OS.StrictMode.ThreadPolicy.Builder().PermitAll().Build();
        Android.OS.StrictMode.SetThreadPolicy(policy);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
