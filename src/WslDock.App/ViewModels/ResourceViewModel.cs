using CommunityToolkit.Mvvm.ComponentModel;
namespace WslDock.App.ViewModels;
public abstract partial class ResourceViewModel(DistributionContext context) : ObservableObject
{
    protected DistributionContext Context { get; } = context;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    protected async Task RunAsync(Func<string, CancellationToken, Task> action)
    {
        if (IsBusy || Context.Name is null) return;
        IsBusy = true; ErrorMessage = null;
        try { await action(Context.Name, CancellationToken.None); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
    protected static void Replace<T>(System.Collections.ObjectModel.ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
}
