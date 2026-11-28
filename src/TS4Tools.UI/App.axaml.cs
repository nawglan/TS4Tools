using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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

            // Last-chance handlers: an exception no command caught (for example one thrown by plugin code) is
            // reported instead of terminating the app and losing unsaved edits. Legacy s4pe (WinForms) got this
            // from the default ThreadException dialog, which lets the user continue.
            Dispatcher.UIThread.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            SettingsService.Instance.SaveFailed += OnSettingsSaveFailed;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Running out of memory leaves the process in an unknown state; let it terminate.
        if (e.Exception is OutOfMemoryException) return;
        Trace.TraceError("Unhandled exception: {0}", e.Exception);
        e.Handled = true;
        ReportError(e.Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Trace.TraceError("Unobserved task exception: {0}", e.Exception);
        e.SetObserved();
        var error = e.Exception.InnerExceptions.Count == 1 ? e.Exception.InnerExceptions[0] : e.Exception;
        Dispatcher.UIThread.Post(() => ReportError(error));
    }

    private void OnSettingsSaveFailed(object? sender, Exception e) =>
        Dispatcher.UIThread.Post(() => ReportError(new IOException($"Could not save settings: {e.Message}", e)));

    private void ReportError(Exception error)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: MainWindowViewModel vm })
            vm.ReportUnhandledError(error);
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Dispatcher.UIThread.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        SettingsService.Instance.SaveFailed -= OnSettingsSaveFailed;
        if (sender is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Exit -= OnExit;
    }
}
