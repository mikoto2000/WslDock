using WslDock.App.Services;
using WslDock.App.ViewModels;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.Application.Tests;

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
        var vm = Create(out var docker, out _);
        vm.SelectedProject = Project(); vm.SelectedService = Service("exited");
        await vm.ServiceStartCommand.ExecuteAsync(null);
        Assert.Equal("container-id", docker.StartedId);
        Assert.NotEmpty(vm.Services);
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
    public async Task Exec_detects_shell_and_launches_for_selected_container()
    {
        var vm = Create(out _, out var exec);
        vm.SelectedProject = Project(); vm.SelectedService = Service("running");
        await vm.ServiceExecCommand.ExecuteAsync(null);
        Assert.Equal(("Ubuntu", "container-id", ContainerShell.Sh), exec.Launched);
    }

    private static ComposeProjectsViewModel Create(out DockerClient docker, out ExecService exec, bool confirmation = true)
    {
        docker = new DockerClient(); exec = new ExecService();
        return new ComposeProjectsViewModel(new DistributionContext { SelectedDistribution = new("Ubuntu") }, new ComposeClient(), new Confirmation(confirmation), new WslDockOptions(), docker, exec) { IsComposeAvailable = true };
    }
    private static DockerComposeProject Project() => new("demo", "running(1)", ["/work/compose.yml"], "/work");
    private static DockerComposeService Service(string state) => new("container-id", "web", "demo-web-1", "nginx", state, state, "80/tcp");
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
        public Task<IReadOnlyList<DockerComposeService>> GetServicesAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DockerComposeService>>([Service("running")]);
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
        public (string Distribution, string Id, ContainerShell Shell)? Launched { get; private set; }
        public Task<ContainerShell?> DetectShellAsync(string d, string id, CancellationToken ct = default) => Task.FromResult<ContainerShell?>(ContainerShell.Sh);
        public Task OpenInteractiveShellAsync(string d, string id, ContainerShell shell, CancellationToken ct = default) { Launched = (d, id, shell); return Task.CompletedTask; }
    }
}
