using WslDock.Domain;

namespace WslDock.App.ViewModels;

public sealed class DistributionContext
{
    private WslDistribution? selectedDistribution;

    public event Action? SelectionChanged;

    public WslDistribution? SelectedDistribution
    {
        get => selectedDistribution;
        set
        {
            if (selectedDistribution == value) return;
            selectedDistribution = value;
            SelectionChanged?.Invoke();
        }
    }

    public string? Name => SelectedDistribution?.Name;
}
