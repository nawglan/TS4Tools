using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views;

namespace TS4Tools.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Build the wrapper registry before any package is opened: built-in wrappers, plugins from the
        // Wrappers folder and the persisted disabled list.
        // Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 87-115 and
        //         legacy_references/Sims4Tools/s4pe/MainForm.cs lines 80-92
        var wrappers = WrapperRegistryService.Initialize(SettingsService.Instance.Settings);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;
            (window.DataContext as MainWindowViewModel)?.ReportWrapperLoadErrors(wrappers.LoadErrors);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
