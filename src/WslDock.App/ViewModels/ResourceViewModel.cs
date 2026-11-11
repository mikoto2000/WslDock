using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WslDock.App.ViewModels;

public abstract partial class ResourceViewModel : ObservableObject, IDisposable
{
    private readonly DistributionContext context;
    private CancellationTokenSource? operation;
    private bool disposed;

    protected ResourceViewModel(DistributionContext context)
    {
        this.context = context;
        context.SelectionChanged += OnDistributionChanged;
    }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    protected bool CanRun() => !disposed && !IsBusy && context.Name is not null;

    protected async Task RunAsync(Func<string, CancellationToken, Task> action)
    {
        if (!CanRun() || context.Name is not { } distribution) return;
        using var current = new CancellationTokenSource();
        operation = current;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            current.Token.ThrowIfCancellationRequested();
            await action(distribution, current.Token);
        }
        catch (Exception ex)
        {
            if (!current.IsCancellationRequested) ErrorMessage = ex.Message;
        }
        finally
        {
            // A distribution change may have started a newer operation.
            if (operation == current)
            {
                operation = null;
                IsBusy = false;
                OperationCompleted();
            }
        }
    }

    private void OnDistributionChanged()
    {
        CancelOperation();
        ErrorMessage = null;
        Reset();
        IsBusy = false;
        NotifyCommands();
    }

    private void CancelOperation()
    {
        var current = operation;
        operation = null;
        current?.Cancel();
    }

    partial void OnIsBusyChanged(bool value) => NotifyCommands();

    protected abstract void NotifyCommands();
    protected abstract void Reset();
    protected virtual void OperationCompleted() { }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        context.SelectionChanged -= OnDistributionChanged;
        CancelOperation();
    }

    protected static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        target.Clear();
        foreach (var value in values) target.Add(value);
    }
}
