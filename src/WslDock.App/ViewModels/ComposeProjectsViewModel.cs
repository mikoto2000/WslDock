using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;
namespace WslDock.App.ViewModels;
public partial class ComposeProjectsViewModel(DistributionContext context, IDockerComposeClient client, IConfirmationService confirmation, WslDockOptions options) : ResourceViewModel(context)
{
    public ObservableCollection<DockerComposeProject> Projects { get; } = [];
    public ObservableCollection<DockerComposeService> Services { get; } = [];
    [ObservableProperty] private bool isComposeAvailable;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(UpCommand), nameof(DownCommand), nameof(RestartCommand), nameof(LogsCommand))] private DockerComposeProject? selectedProject;
    [ObservableProperty] private string? logs;
    partial void OnSelectedProjectChanged(DockerComposeProject? value) { if (value is not null) _ = LoadDetailsAsync(); else Services.Clear(); }
    [RelayCommand] private Task RefreshAsync() => RunAsync(async (d, ct) => { IsComposeAvailable = await client.IsAvailableAsync(d, ct); if (!IsComposeAvailable) { Projects.Clear(); Services.Clear(); ErrorMessage = "Docker Compose v2 is not available in this distribution."; return; } Replace(Projects, await client.GetProjectsAsync(d, ct)); });
    private Task LoadDetailsAsync() => RunAsync(async (d, ct) => Replace(Services, await client.GetServicesAsync(d, SelectedProject!, ct)));
    [RelayCommand(CanExecute = nameof(CanOperate))] private Task UpAsync() => Operate((d, p, ct) => client.UpAsync(d, p, ct));
    [RelayCommand(CanExecute = nameof(CanOperate))] private Task RestartAsync() => Operate((d, p, ct) => client.RestartAsync(d, p, ct));
    [RelayCommand(CanExecute = nameof(CanOperate))] private Task LogsAsync() => RunAsync(async (d, ct) => Logs = await client.GetLogsAsync(d, SelectedProject!, options.LogTailLines, ct));
    [RelayCommand(CanExecute = nameof(CanOperate))] private Task DownAsync() => confirmation.Confirm($"Stop and remove Compose project \"{SelectedProject!.Name}\" containers and networks? Volumes and images are kept.") ? Operate((d, p, ct) => client.DownAsync(d, p, ct)) : Task.CompletedTask;
    private bool CanOperate() => !IsBusy && SelectedProject is not null && IsComposeAvailable;
    private Task Operate(Func<string, DockerComposeProject, CancellationToken, Task> action) => RunAsync(async (d, ct) => { await action(d, SelectedProject!, ct); Replace(Projects, await client.GetProjectsAsync(d, ct)); if (SelectedProject is not null) Replace(Services, await client.GetServicesAsync(d, SelectedProject, ct)); });
    public void Reset() { Projects.Clear(); Services.Clear(); SelectedProject = null; Logs = null; ErrorMessage = null; IsComposeAvailable = false; }
    protected override void BusyChanged() { RefreshCommand.NotifyCanExecuteChanged(); UpCommand.NotifyCanExecuteChanged(); DownCommand.NotifyCanExecuteChanged(); RestartCommand.NotifyCanExecuteChanged(); LogsCommand.NotifyCanExecuteChanged(); }
}
