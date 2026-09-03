using Microsoft.Maui.Controls;

namespace VBotMauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
