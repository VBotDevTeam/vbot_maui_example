using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using VBot.Phone.SDK.Maui;
using VBotMauiApp.ViewModels;
using VBotMauiApp.Views;

namespace VBotMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // VBot Phone SDK
        builder.Services.AddVBotPhone();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<CallViewModel>();

        // Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CallPage>();

        return builder.Build();
    }
}
