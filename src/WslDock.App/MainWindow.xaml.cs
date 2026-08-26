using System.Windows;
using WslDock.App.ViewModels;

namespace WslDock.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await viewModel.InitializeAsync();
    }
}
