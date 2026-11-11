using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.ViewModels;

public partial class MainViewModel(
    IWslService wslService,
    WslDockOptions options,
    DistributionContext distributionContext,
    ContainersViewModel containersViewModel,
    ImagesViewModel imagesViewModel,
    VolumesViewModel volumesViewModel,
    NetworksViewModel networksViewModel,
    ComposeProjectsViewModel composeViewModel) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource shutdown = new();
    private Task containerRefresh = Task.CompletedTask;
    private bool initializing;

    public ObservableCollection<WslDistribution> Distributions { get; } = [];
    public ContainersViewModel ContainersViewModel { get; } = containersViewModel;
    public ImagesViewModel ImagesViewModel { get; } = imagesViewModel;
    public VolumesViewModel VolumesViewModel { get; } = volumesViewModel;
    public NetworksViewModel NetworksViewModel { get; } = networksViewModel;
    public ComposeProjectsViewModel ComposeViewModel { get; } = composeViewModel;

    [ObservableProperty] private WslDistribution? selectedDistribution;
    [ObservableProperty] private string? errorMessage;

    public async Task InitializeAsync()
    {
        if (initializing) return;
        var token = shutdown.Token;
        initializing = true;
        ErrorMessage = null;
        try
        {
            var distributions = await wslService.GetDistributionsAsync(token);
            token.ThrowIfCancellationRequested();
            Distributions.Clear();
            foreach (var distribution in distributions) Distributions.Add(distribution);
            SelectedDistribution = distributions.FirstOrDefault(x => x.Name == options.DefaultDistribution)
                ?? distributions.FirstOrDefault();
            await containerRefresh;
        }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested) ErrorMessage = exception.Message;
        }
        finally
        {
            initializing = false;
        }
    }

    partial void OnSelectedDistributionChanged(WslDistribution? value)
    {
        distributionContext.SelectedDistribution = value;
        containerRefresh = ContainersViewModel.RefreshAsync();
    }

    public void Dispose()
    {
        shutdown.Cancel();
        shutdown.Dispose();
    }
}
