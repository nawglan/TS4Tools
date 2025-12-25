using Avalonia.Controls;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
