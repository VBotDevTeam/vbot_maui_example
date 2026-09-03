using Microsoft.Maui.Controls;
using VBotMauiApp.Views;

namespace VBotMauiApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(CallPage), typeof(CallPage));
    }
}
