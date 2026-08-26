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
    public ObservableCollection<DockerComposeProject> Projects { get; } = [];
    public ObservableCollection<DockerComposeService> Services { get; } = [];

    [ObservableProperty] private bool isComposeAvailable;
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
        if (value is not null) _ = LoadDetailsAsync(); else Services.Clear();
    }

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(async (distribution, token) =>
    {
        IsComposeAvailable = await composeClient.IsAvailableAsync(distribution, token);
        if (!IsComposeAvailable)
        {
            Projects.Clear(); Services.Clear();
            ErrorMessage = "Docker Compose v2 is not available in this distribution.";
            return;
        }
        Replace(Projects, await composeClient.GetProjectsAsync(distribution, token));
    });

    private Task LoadDetailsAsync() => RunAsync(async (distribution, token) =>
        Replace(Services, await composeClient.GetServicesAsync(distribution, SelectedProject!, token)));

    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task UpAsync() => OperateProject((d, p, ct) => composeClient.UpAsync(d, p, ct));
    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task RestartAsync() => OperateProject((d, p, ct) => composeClient.RestartAsync(d, p, ct));
    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task LogsAsync() => RunAsync(async (d, ct) => Logs = await composeClient.GetLogsAsync(d, SelectedProject!, options.LogTailLines, ct));
    [RelayCommand(CanExecute = nameof(CanOperateProject))]
    private Task DownAsync() => confirmation.Confirm($"Stop and remove Compose project \"{SelectedProject!.Name}\" containers and networks? Volumes and images are kept.")
        ? OperateProject((d, p, ct) => composeClient.DownAsync(d, p, ct)) : Task.CompletedTask;
    private bool CanOperateProject() => !IsBusy && SelectedProject is not null && IsComposeAvailable;

    [RelayCommand(CanExecute = nameof(CanStartService))]
    private Task ServiceStartAsync() => OperateService((d, id, ct) => dockerClient.StartAsync(d, id, ct));
    private bool CanStartService() => CanSelectService() && !IsRunning(SelectedService!);

    [RelayCommand(CanExecute = nameof(CanStopService))]
    private Task ServiceStopAsync() => OperateService((d, id, ct) => dockerClient.StopAsync(d, id, ct));
    private bool CanStopService() => CanSelectService() && IsRunning(SelectedService!);

    [RelayCommand(CanExecute = nameof(CanSelectService))]
    private Task ServiceRestartAsync() => OperateService((d, id, ct) => dockerClient.RestartAsync(d, id, ct));

    [RelayCommand(CanExecute = nameof(CanRemoveService))]
    private Task ServiceRemoveAsync() => confirmation.Confirm($"Remove Compose container \"{SelectedService!.ContainerName}\"?")
        ? OperateService((d, id, ct) => dockerClient.RemoveAsync(d, id, ct)) : Task.CompletedTask;
    private bool CanRemoveService() => CanSelectService() && !IsRunning(SelectedService!);

    [RelayCommand(CanExecute = nameof(CanSelectService))]
    private Task ServiceLogsAsync() => RunAsync(async (d, ct) =>
        Logs = await dockerClient.GetLogsAsync(d, SelectedService!.Id, options.LogTailLines, ct));

    [RelayCommand(CanExecute = nameof(CanExecService))]
    private Task ServiceExecAsync() => RunAsync(async (d, ct) =>
    {
        var shell = await execService.DetectShellAsync(d, SelectedService!.Id, ct);
        if (shell is null) throw new InvalidOperationException("This container does not appear to provide /bin/bash or /bin/sh.");
        await execService.OpenInteractiveShellAsync(d, SelectedService.Id, shell.Value, ct);
    });
    private bool CanExecService() => CanSelectService() && IsRunning(SelectedService!);
    private bool CanSelectService() => !IsBusy && SelectedProject is not null && SelectedService is not null;

    private Task OperateProject(Func<string, DockerComposeProject, CancellationToken, Task> action) => RunAsync(async (d, ct) =>
    {
        await action(d, SelectedProject!, ct);
        Replace(Projects, await composeClient.GetProjectsAsync(d, ct));
        if (SelectedProject is not null) Replace(Services, await composeClient.GetServicesAsync(d, SelectedProject, ct));
    });

    private Task OperateService(Func<string, string, CancellationToken, Task> action) => RunAsync(async (d, ct) =>
    {
        await action(d, SelectedService!.Id, ct);
        if (SelectedProject is not null) Replace(Services, await composeClient.GetServicesAsync(d, SelectedProject, ct));
    });

    private static bool IsRunning(DockerComposeService service) => service.State.Equals("running", StringComparison.OrdinalIgnoreCase);

    public void Reset() { Projects.Clear(); Services.Clear(); SelectedProject = null; SelectedService = null; Logs = null; ErrorMessage = null; IsComposeAvailable = false; }

    protected override void BusyChanged()
    {
        RefreshCommand.NotifyCanExecuteChanged(); UpCommand.NotifyCanExecuteChanged(); DownCommand.NotifyCanExecuteChanged(); RestartCommand.NotifyCanExecuteChanged(); LogsCommand.NotifyCanExecuteChanged();
        ServiceStartCommand.NotifyCanExecuteChanged(); ServiceStopCommand.NotifyCanExecuteChanged(); ServiceRestartCommand.NotifyCanExecuteChanged(); ServiceRemoveCommand.NotifyCanExecuteChanged(); ServiceLogsCommand.NotifyCanExecuteChanged(); ServiceExecCommand.NotifyCanExecuteChanged();
    }
}
