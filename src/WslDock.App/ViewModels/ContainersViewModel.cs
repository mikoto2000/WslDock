using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.ViewModels;

public partial class ContainersViewModel(
    DistributionContext context,
    IDockerClient client,
    WslDockOptions options,
    IConfirmationService confirmation,
    IContainerExecService execService) : ResourceViewModel(context)
{
    public ObservableCollection<DockerContainer> Containers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand), nameof(RemoveCommand), nameof(LoadLogsCommand), nameof(ExecCommand))]
    private DockerContainer? selectedContainer;

    [ObservableProperty] private DockerEngineStatus engineStatus = DockerEngineStatus.Unavailable;
    [ObservableProperty] private string? logs;

    [RelayCommand(CanExecute = nameof(CanRun))]
    internal Task RefreshAsync() => RunAsync(RefreshCoreAsync);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartAsync() => CanStart() ? OperateAsync(client.StartAsync) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private Task StopAsync() => CanStop() ? OperateAsync(client.StopAsync) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task RestartAsync() => OperateAsync(client.RestartAsync);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveAsync()
    {
        if (!CanRemove()) return Task.CompletedTask;
        var container = SelectedContainer!;
        return RunAsync(async (distribution, token) =>
        {
            if (!confirmation.Confirm($"Remove container \"{container.Name}\"?")) return;
            token.ThrowIfCancellationRequested();
            await client.RemoveAsync(distribution, container.Id, token);
            await RefreshCoreAsync(distribution, token);
        });
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task LoadLogsAsync()
    {
        if (!CanOperate()) return Task.CompletedTask;
        var id = SelectedContainer!.Id;
        return RunAsync(async (distribution, token) =>
        {
            var output = await client.GetLogsAsync(distribution, id, options.LogTailLines, token);
            token.ThrowIfCancellationRequested();
            Logs = output;
        });
    }

    [RelayCommand(CanExecute = nameof(CanExec))]
    private Task ExecAsync()
    {
        if (!CanExec()) return Task.CompletedTask;
        var id = SelectedContainer!.Id;
        return RunAsync((distribution, token) => execService.OpenInteractiveShellAsync(distribution, id, token));
    }

    private bool CanOperate() => CanRun() && SelectedContainer is not null;
    private bool CanStart() => CanOperate() && SelectedContainer!.State.CanStart();
    private bool CanStop() => CanOperate() && SelectedContainer!.State.CanStop();
    private bool CanRemove() => CanOperate() && SelectedContainer!.State.CanRemove();
    private bool CanExec() => CanOperate() && SelectedContainer!.State == DockerContainerState.Running;

    private Task OperateAsync(Func<string, string, CancellationToken, Task> action)
    {
        if (!CanOperate()) return Task.CompletedTask;
        var id = SelectedContainer!.Id;
        return RunAsync(async (distribution, token) =>
        {
            await action(distribution, id, token);
            await RefreshCoreAsync(distribution, token);
        });
    }

    private async Task RefreshCoreAsync(string distribution, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var status = await client.GetStatusAsync(distribution, token);
        token.ThrowIfCancellationRequested();
        EngineStatus = status;

        IReadOnlyList<DockerContainer> containers = status == DockerEngineStatus.Running
            ? await client.GetContainersAsync(distribution, token)
            : [];
        Replace(Containers, containers, token);
        SelectedContainer = null;
    }

    protected override void Reset()
    {
        Containers.Clear();
        SelectedContainer = null;
        EngineStatus = DockerEngineStatus.Unavailable;
        Logs = null;
    }

    protected override void NotifyCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RestartCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        LoadLogsCommand.NotifyCanExecuteChanged();
        ExecCommand.NotifyCanExecuteChanged();
    }
}
