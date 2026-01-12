using Avalonia.Controls;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        // Wire up the advanced filter panel
        FilterPanel.FilterChanged += (sender, args) =>
        {
            viewModel.ApplyAdvancedFilter(args);
        };

        Closed += OnWindowClosed;
    }

    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        if (DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}
