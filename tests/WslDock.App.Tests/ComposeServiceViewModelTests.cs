using WslDock.App.Services;
using WslDock.App.ViewModels;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.Tests;

public sealed class ComposeServiceViewModelTests
{
    [Fact]
    public async Task Commands_follow_selected_service_state()
    {
        var vm = Create(out _, out _);
        Assert.False(vm.ServiceStartCommand.CanExecute(null));
        vm.SelectedProject = Project();
        await WaitUntilAsync(() => !vm.IsBusy);
        vm.SelectedService = Service("running");
        Assert.False(vm.ServiceStartCommand.CanExecute(null));
        Assert.True(vm.ServiceStopCommand.CanExecute(null));
        Assert.True(vm.ServiceExecCommand.CanExecute(null));
        vm.SelectedService = Service("exited");
        Assert.True(vm.ServiceStartCommand.CanExecute(null));
        Assert.False(vm.ServiceStopCommand.CanExecute(null));
        Assert.False(vm.ServiceExecCommand.CanExecute(null));
    }

    [Fact]
    public async Task Start_uses_container_id_and_refreshes_services()
    {
        var composeClient = new ComposeClient();
        using var vm = Create(out var docker, out _, composeClient: composeClient);
        var stoppedService = Service("exited");
        var runningService = Service("running");
        composeClient.LoadServices = _ => Task.FromResult<IReadOnlyList<DockerComposeService>>(
            [docker.StartedId == stoppedService.Id ? runningService : stoppedService]);
        vm.SelectedProject = Project();
        vm.SelectedService = Assert.Single(vm.Services);
        Assert.Equal(DockerContainerState.Exited, vm.SelectedService.State);
        Assert.True(vm.ServiceStartCommand.CanExecute(null));

        await vm.ServiceStartCommand.ExecuteAsync(null);

        Assert.Equal(stoppedService.Id, docker.StartedId);
        Assert.Equal(runningService, Assert.Single(vm.Services));
    }

    [Fact]
    public async Task Remove_requires_confirmation_and_uses_container_id()
    {
        var vm = Create(out var docker, out _, confirmation: true);
        vm.SelectedProject = Project(); vm.SelectedService = Service("exited");
        await vm.ServiceRemoveCommand.ExecuteAsync(null);
        Assert.Equal("container-id", docker.RemovedId);
    }

    [Fact]
    public async Task Declined_compose_service_confirmation_does_not_remove()
    {
        var composeClient = new ComposeClient
        {
            LoadServices = _ => Task.FromResult<IReadOnlyList<DockerComposeService>>([Service("exited")])
        };
        using var vm = Create(out var docker, out _, confirmation: false, composeClient: composeClient);
        vm.SelectedProject = Project();
        vm.SelectedService = Assert.Single(vm.Services);
        Assert.True(vm.ServiceRemoveCommand.CanExecute(null));

        await vm.ServiceRemoveCommand.ExecuteAsync(null);

        Assert.Null(docker.RemovedId);
    }

    [Fact]
    public async Task Exec_detects_shell_and_launches_for_selected_container()
    {
        var vm = Create(out _, out var exec);
        vm.SelectedProject = Project(); vm.SelectedService = Service("running");
        await vm.ServiceExecCommand.ExecuteAsync(null);
        Assert.Equal(("Ubuntu", "container-id"), exec.Launched);
    }

    [Fact]
    public async Task Selection_changes_while_loading_details_show_only_the_latest_project()
    {
        var oldDetails = new TaskCompletionSource<IReadOnlyList<DockerComposeService>>();
        var oldProject = Project();
        var newProject = oldProject with { Name = "other" };
        var newService = Service("running") with { Id = "other-container" };
        var client = new ComposeClient
        {
            LoadServices = project => project == oldProject
                ? oldDetails.Task
                : Task.FromResult<IReadOnlyList<DockerComposeService>>([newService])
        };
        using var vm = Create(out _, out _, composeClient: client);

        vm.SelectedProject = oldProject;
        vm.SelectedProject = newProject;
        Assert.Empty(vm.Services);

        oldDetails.SetResult([Service("exited")]);
        await WaitUntilAsync(() => vm.Services.Count == 1 && !vm.IsBusy);

        Assert.Equal(newService, Assert.Single(vm.Services));
        Assert.Equal(newProject, vm.SelectedProject);
    }

    [Theory]
    [InlineData("paused", true)]
    [InlineData("restarting", false)]
    public void Active_service_cannot_be_started_removed_or_used_for_exec(string state, bool canStop)
    {
        using var vm = Create(out _, out _);
        vm.SelectedProject = Project();
        vm.SelectedService = Service(state);

        Assert.False(vm.ServiceStartCommand.CanExecute(null));
        Assert.False(vm.ServiceRemoveCommand.CanExecute(null));
        Assert.False(vm.ServiceExecCommand.CanExecute(null));
        Assert.Equal(canStop, vm.ServiceStopCommand.CanExecute(null));
    }

    private static ComposeProjectsViewModel Create(out DockerClient docker, out ExecService exec, bool confirmation = true, ComposeClient? composeClient = null)
    {
        docker = new DockerClient(); exec = new ExecService();
        return new ComposeProjectsViewModel(new DistributionContext { SelectedDistribution = new("Ubuntu") }, composeClient ?? new ComposeClient(), new Confirmation(confirmation), new WslDockOptions(), docker, exec) { IsComposeAvailable = true };
    }
    private static DockerComposeProject Project() => new("demo", "running(1)", ["/work/compose.yml"], "/work");
    private static DockerComposeService Service(string state) => new("container-id", "web", "demo-web-1", "nginx", Enum.Parse<DockerContainerState>(state, ignoreCase: true), state, "80/tcp");
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++) await Task.Delay(10);
        Assert.True(condition());
    }

    private sealed class Confirmation(bool answer) : IConfirmationService { public bool Confirm(string message) => answer; }
    private sealed class ComposeClient : IDockerComposeClient
    {
        public Task<bool> IsAvailableAsync(string d, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<DockerComposeProject>> GetProjectsAsync(string d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DockerComposeProject>>([Project()]);
        public Func<DockerComposeProject, Task<IReadOnlyList<DockerComposeService>>>? LoadServices { get; set; }
        public Task<IReadOnlyList<DockerComposeService>> GetServicesAsync(string d, DockerComposeProject p, CancellationToken ct = default) =>
            LoadServices?.Invoke(p) ?? Task.FromResult<IReadOnlyList<DockerComposeService>>([Service("running")]);
        public Task UpAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Task.CompletedTask;
        public Task DownAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestartAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> GetLogsAsync(string d, DockerComposeProject p, int lines, CancellationToken ct = default) => Task.FromResult("");
    }
    private sealed class DockerClient : IDockerClient
    {
        public string? StartedId { get; private set; } public string? RemovedId { get; private set; }
        public Task<DockerEngineStatus> GetStatusAsync(string d, CancellationToken ct = default) => Task.FromResult(DockerEngineStatus.Running);
        public Task<IReadOnlyList<DockerContainer>> GetContainersAsync(string d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DockerContainer>>([]);
        public Task StartAsync(string d, string id, CancellationToken ct = default) { StartedId = id; return Task.CompletedTask; }
        public Task StopAsync(string d, string id, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestartAsync(string d, string id, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(string d, string id, CancellationToken ct = default) { RemovedId = id; return Task.CompletedTask; }
        public Task<string> GetLogsAsync(string d, string id, int lines, CancellationToken ct = default) => Task.FromResult("service logs");
    }
    private sealed class ExecService : IContainerExecService
    {
        public (string Distribution, string Id)? Launched { get; private set; }
        public Task OpenInteractiveShellAsync(string d, string id, CancellationToken ct = default) { Launched = (d, id); return Task.CompletedTask; }
    }
}
