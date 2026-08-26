using CommunityToolkit.Mvvm.ComponentModel;
using WslDock.Domain;
namespace WslDock.App.ViewModels;
public partial class DistributionContext : ObservableObject
{
    [ObservableProperty] private WslDistribution? selectedDistribution;
    public string? Name => SelectedDistribution?.Name;
    partial void OnSelectedDistributionChanged(WslDistribution? value) => OnPropertyChanged(nameof(Name));
}
