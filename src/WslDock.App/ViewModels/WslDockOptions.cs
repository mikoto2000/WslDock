namespace WslDock.App.ViewModels;

public sealed class WslDockOptions
{
    public string? DefaultDistribution { get; set; }
    public int LogTailLines { get; set; } = 500;
}
