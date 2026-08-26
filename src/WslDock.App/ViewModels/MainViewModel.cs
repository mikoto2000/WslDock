using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.Application;
using WslDock.Domain;
using WslDock.App.Services;

namespace WslDock.App.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IWslService wslService;
    private readonly IDockerClient dockerClient;
    private readonly WslDockOptions options;
    private readonly DistributionContext? distributionContext;
    private readonly IConfirmationService? confirmation;
    private readonly CancellationTokenSource shutdown = new();

    public ImagesViewModel? ImagesViewModel { get; }
    public VolumesViewModel? VolumesViewModel { get; }
    public NetworksViewModel? NetworksViewModel { get; }
    public ComposeProjectsViewModel? ComposeViewModel { get; }

    public MainViewModel(IWslService wslService, IDockerClient dockerClient, WslDockOptions options)
    {
        this.wslService = wslService; this.dockerClient = dockerClient; this.options = options;
    }

    public MainViewModel(IWslService wslService, IDockerClient dockerClient, WslDockOptions options, IConfirmationService confirmation)
        : this(wslService, dockerClient, options) => this.confirmation = confirmation;

    public MainViewModel(IWslService wslService, IDockerClient dockerClient, WslDockOptions options, DistributionContext distributionContext,
        ImagesViewModel imagesViewModel, VolumesViewModel volumesViewModel, NetworksViewModel networksViewModel, ComposeProjectsViewModel composeViewModel,
        IConfirmationService confirmation)
        : this(wslService, dockerClient, options)
    {
        this.distributionContext = distributionContext;
        this.confirmation = confirmation;
        ImagesViewModel = imagesViewModel; VolumesViewModel = volumesViewModel; NetworksViewModel = networksViewModel; ComposeViewModel = composeViewModel;
    }

    public ObservableCollection<WslDistribution> Distributions { get; } = [];
    public ObservableCollection<DockerContainer> Containers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand), nameof(RemoveCommand), nameof(LoadLogsCommand))]
    private bool isBusy;

    [ObservableProperty]
    private WslDistribution? selectedDistribution;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand), nameof(RemoveCommand), nameof(LoadLogsCommand))]
    private DockerContainer? selectedContainer;

    [ObservableProperty]
    private DockerEngineStatus engineStatus = DockerEngineStatus.Unavailable;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? logs;

    public async Task InitializeAsync()
    {
        await RunBusyAsync(async token =>
        {
            var distributions = await wslService.GetDistributionsAsync(token);
            Replace(Distributions, distributions);
            SelectedDistribution = distributions.FirstOrDefault(x => x.Name == options.DefaultDistribution) ?? distributions.FirstOrDefault();
            if (SelectedDistribution is not null) await RefreshCoreAsync(token);
        });
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private Task RefreshAsync() => RunBusyAsync(RefreshCoreAsync);
    private bool CanRefresh() => !IsBusy && SelectedDistribution is not null;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartAsync() => OperateAsync((d, id, ct) => dockerClient.StartAsync(d, id, ct));
    private bool CanStart() => !IsBusy && SelectedContainer is { State: not DockerContainerState.Running } && SelectedDistribution is not null;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private Task StopAsync() => OperateAsync((d, id, ct) => dockerClient.StopAsync(d, id, ct));
    private bool CanStop() => !IsBusy && SelectedContainer?.State is DockerContainerState.Running or DockerContainerState.Paused && SelectedDistribution is not null;

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task RestartAsync() => OperateAsync((d, id, ct) => dockerClient.RestartAsync(d, id, ct));

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveAsync()
    {
        if (confirmation is null || !confirmation.Confirm($"Remove container \"{SelectedContainer!.Name}\"?")) return Task.CompletedTask;
        return OperateAsync((d, id, ct) => dockerClient.RemoveAsync(d, id, ct));
    }
    private bool CanRemove() => !IsBusy && SelectedDistribution is not null && SelectedContainer is { State: not DockerContainerState.Running and not DockerContainerState.Paused and not DockerContainerState.Restarting };

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task LoadLogsAsync() => RunBusyAsync(async token =>
        Logs = await dockerClient.GetLogsAsync(SelectedDistribution!.Name, SelectedContainer!.Id, options.LogTailLines, token));

    private bool CanOperate() => !IsBusy && SelectedContainer is not null && SelectedDistribution is not null;

    private Task OperateAsync(Func<string, string, CancellationToken, Task> action) => RunBusyAsync(async token =>
    {
        await action(SelectedDistribution!.Name, SelectedContainer!.Id, token);
        await RefreshCoreAsync(token);
    });

    private async Task RefreshCoreAsync(CancellationToken token)
    {
        if (SelectedDistribution is null) return;
        EngineStatus = await dockerClient.GetStatusAsync(SelectedDistribution.Name, token);
        if (EngineStatus == DockerEngineStatus.Running)
            Replace(Containers, await dockerClient.GetContainersAsync(SelectedDistribution.Name, token));
        else
            Containers.Clear();
    }

    private async Task RunBusyAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try { await action(shutdown.Token); }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    partial void OnSelectedDistributionChanged(WslDistribution? value)
    {
        if (distributionContext is not null) distributionContext.SelectedDistribution = value;
        ImagesViewModel?.Reset(); VolumesViewModel?.Reset(); NetworksViewModel?.Reset(); ComposeViewModel?.Reset();
        SelectedContainer = null;
        RefreshCommand.NotifyCanExecuteChanged();
        if (value is not null && !IsBusy) _ = RefreshCommand.ExecuteAsync(null);
    }

    public void Dispose() { shutdown.Cancel(); shutdown.Dispose(); }
}
