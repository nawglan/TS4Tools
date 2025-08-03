using CommunityToolkit.Mvvm.ComponentModel;

namespace TS4Tools.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}
