using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.ViewModels;

public partial class ComposeProjectsViewModel(
    DistributionContext context,
    IDockerComposeClient composeClient,
    IConfirmationService confirmation,
    WslDockOptions options,
    IDockerClient dockerClient,
    IContainerExecService execService) : ResourceViewModel(context)
{
    private bool detailsPending;

    public ObservableCollection<DockerComposeProject> Projects { get; } = [];
    public ObservableCollection<DockerComposeService> Services { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpCommand), nameof(DownCommand), nameof(RestartCommand), nameof(LogsCommand))]
    private bool isComposeAvailable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpCommand), nameof(DownCommand), nameof(RestartCommand), nameof(LogsCommand))]
    private DockerComposeProject? selectedProject;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ServiceStartCommand), nameof(ServiceStopCommand), nameof(ServiceRestartCommand), nameof(ServiceRemoveCommand), nameof(ServiceLogsCommand), nameof(ServiceExecCommand))]
    private DockerComposeService? selectedService;

    [ObservableProperty] private string? logs;

    partial void OnSelectedProjectChanged(DockerComposeProject? value)
    {
        SelectedService = null;
        Services.Clear();
        Logs = null;
        detailsPending = value is not null;
        if (!IsBusy) OperationCompleted();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task RefreshAsync() => RunAsync(async (distribution, token) =>
    {
        var available = await composeClient.IsAvailableAsync(distribution, token);
        token.ThrowIfCancellationRequested();
        IsComposeAvailable = available;
        if (!available)
        {
            Reset();
            ErrorMessage = "Docker Compose v2 is not available in this distribution.";
            return;
        }

        await RefreshProjectsAsync(distribution, token);
    });

    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task UpAsync() => OperateProjectAsync(composeClient.UpAsync);

    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task RestartAsync() => OperateProjectAsync(composeClient.RestartAsync);

    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task DownAsync()
    {
        if (!CanOperateProject()) return Task.CompletedTask;
        var project = SelectedProject!;
        return RunAsync(async (distribution, token) =>
        {
            if (!confirmation.Confirm($"Stop and remove Compose project \"{project.Name}\" containers and networks? Volumes and images are kept.")) return;
            token.ThrowIfCancellationRequested();
            await composeClient.DownAsync(distribution, project, token);
            await RefreshProjectsAsync(distribution, token);
        });
    }

    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task LogsAsync()
    {
        if (!CanOperateProject()) return Task.CompletedTask;
        var project = SelectedProject!;
        return RunAsync(async (distribution, token) =>
        {
            var output = await composeClient.GetLogsAsync(distribution, project, options.LogTailLines, token);
            token.ThrowIfCancellationRequested();
            if (ReferenceEquals(SelectedProject, project)) Logs = output;
        });
    }

    [RelayCommand(CanExecute = nameof(CanStartService))]
    private Task ServiceStartAsync() => CanStartService() ? OperateServiceAsync(dockerClient.StartAsync) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanStopService))]
    private Task ServiceStopAsync() => CanStopService() ? OperateServiceAsync(dockerClient.StopAsync) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanSelectService))]
    private Task ServiceRestartAsync() => OperateServiceAsync(dockerClient.RestartAsync);

    [RelayCommand(CanExecute = nameof(CanRemoveService))]
    private Task ServiceRemoveAsync()
    {
        if (!CanRemoveService()) return Task.CompletedTask;
        var service = SelectedService!;
        var project = SelectedProject!;
        return RunAsync(async (distribution, token) =>
        {
            if (!confirmation.Confirm($"Remove Compose container \"{service.ContainerName}\"?")) return;
            token.ThrowIfCancellationRequested();
            await dockerClient.RemoveAsync(distribution, service.Id, token);
            await RefreshServicesAsync(distribution, project, token);
        });
    }

    [RelayCommand(CanExecute = nameof(CanSelectService))]
    private Task ServiceLogsAsync()
    {
        if (!CanSelectService()) return Task.CompletedTask;
        var service = SelectedService!;
        return RunAsync(async (distribution, token) =>
        {
            var output = await dockerClient.GetLogsAsync(distribution, service.Id, options.LogTailLines, token);
            token.ThrowIfCancellationRequested();
            if (ReferenceEquals(SelectedService, service)) Logs = output;
        });
    }

    [RelayCommand(CanExecute = nameof(CanExecService))]
    private Task ServiceExecAsync()
    {
        if (!CanExecService()) return Task.CompletedTask;
        var id = SelectedService!.Id;
        return RunAsync((distribution, token) => execService.OpenInteractiveShellAsync(distribution, id, token));
    }

    private bool CanOperateProject() => CanRun() && IsComposeAvailable && SelectedProject is not null;
    private bool CanSelectService() => CanOperateProject() && SelectedService is not null;
    private bool CanStartService() => CanSelectService() && SelectedService!.State.CanStart();
    private bool CanStopService() => CanSelectService() && SelectedService!.State.CanStop();
    private bool CanRemoveService() => CanSelectService() && SelectedService!.State.CanRemove();
    private bool CanExecService() => CanSelectService() && SelectedService!.State == DockerContainerState.Running;

    private Task OperateProjectAsync(Func<string, DockerComposeProject, CancellationToken, Task> action)
    {
        if (!CanOperateProject()) return Task.CompletedTask;
        var project = SelectedProject!;
        return RunAsync(async (distribution, token) =>
        {
            await action(distribution, project, token);
            await RefreshProjectsAsync(distribution, token);
        });
    }

    private Task OperateServiceAsync(Func<string, string, CancellationToken, Task> action)
    {
        if (!CanSelectService()) return Task.CompletedTask;
        var project = SelectedProject!;
        var id = SelectedService!.Id;
        return RunAsync(async (distribution, token) =>
        {
            await action(distribution, id, token);
            await RefreshServicesAsync(distribution, project, token);
        });
    }

    private async Task RefreshProjectsAsync(string distribution, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var projects = await composeClient.GetProjectsAsync(distribution, token);
        token.ThrowIfCancellationRequested();
        var name = SelectedProject?.Name;
        Replace(Projects, projects, token);
        SelectedProject = projects.FirstOrDefault(project => project.Name == name);
        if (SelectedProject is { } selected)
        {
            detailsPending = false;
            await RefreshServicesAsync(distribution, selected, token);
        }
    }

    private async Task RefreshServicesAsync(string distribution, DockerComposeProject project, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!ReferenceEquals(project, SelectedProject)) return;
        var services = await composeClient.GetServicesAsync(distribution, project, token);
        token.ThrowIfCancellationRequested();
        if (!ReferenceEquals(project, SelectedProject)) return;
        Replace(Services, services, token);
        SelectedService = null;
    }

    protected override void OperationCompleted()
    {
        if (!detailsPending || SelectedProject is not { } project) return;
        detailsPending = false;
        _ = RunAsync((distribution, token) => RefreshServicesAsync(distribution, project, token));
    }

    protected override void Reset()
    {
        SelectedProject = null;
        Projects.Clear();
        Services.Clear();
        SelectedService = null;
        Logs = null;
        IsComposeAvailable = false;
        detailsPending = false;
    }

    protected override void NotifyCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        UpCommand.NotifyCanExecuteChanged();
        DownCommand.NotifyCanExecuteChanged();
        RestartCommand.NotifyCanExecuteChanged();
        LogsCommand.NotifyCanExecuteChanged();
        ServiceStartCommand.NotifyCanExecuteChanged();
        ServiceStopCommand.NotifyCanExecuteChanged();
        ServiceRestartCommand.NotifyCanExecuteChanged();
        ServiceRemoveCommand.NotifyCanExecuteChanged();
        ServiceLogsCommand.NotifyCanExecuteChanged();
        ServiceExecCommand.NotifyCanExecuteChanged();
    }
}
