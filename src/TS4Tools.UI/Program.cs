using Avalonia;
using TS4Tools.UI.Services;

namespace TS4Tools.UI;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Settings are read with async file I/O. Main must stay synchronous ([STAThread] is lost
        // across awaits in an async Main), so we wait for the load here, before any UI thread or
        // synchronization context exists - there is nothing to block and no deadlock risk.
        // Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings (loaded at startup)
        SettingsService.InitializeAsync().GetAwaiter().GetResult();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
