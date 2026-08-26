using WslDock.App.ViewModels;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.Application.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task Initialize_selects_configured_distribution_and_loads_containers()
    {
        var docker = new FakeDockerClient();
        var vm = new MainViewModel(new FakeWslService(), docker, new WslDockOptions { DefaultDistribution = "Debian" });
        await vm.InitializeAsync();
        Assert.Equal("Debian", vm.SelectedDistribution?.Name);
        Assert.Single(vm.Containers);
        Assert.Equal(DockerEngineStatus.Running, vm.EngineStatus);
    }

    [Fact]
    public async Task Start_refreshes_list_after_success()
    {
        var docker = new FakeDockerClient();
        var vm = new MainViewModel(new FakeWslService(), docker, new WslDockOptions());
        await vm.InitializeAsync();
        vm.SelectedContainer = vm.Containers[0] with { State = DockerContainerState.Exited };
        await vm.StartCommand.ExecuteAsync(null);
        Assert.Equal(1, docker.Starts);
        Assert.True(docker.ListCalls >= 2);
    }

    [Fact]
    public async Task Running_container_disables_start_and_enables_stop()
    {
        var vm = new MainViewModel(new FakeWslService(), new FakeDockerClient(), new WslDockOptions());
        await vm.InitializeAsync();
        vm.SelectedContainer = vm.Containers[0];
        Assert.False(vm.StartCommand.CanExecute(null));
        Assert.True(vm.StopCommand.CanExecute(null));
        Assert.False(vm.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Remove_requires_confirmation_and_refreshes()
    {
        var docker = new FakeDockerClient();
        var vm = new MainViewModel(new FakeWslService(), docker, new WslDockOptions(), new Confirmation(true));
        await vm.InitializeAsync();
        vm.SelectedContainer = vm.Containers[0] with { State = DockerContainerState.Exited };
        await vm.RemoveCommand.ExecuteAsync(null);
        Assert.Equal(1, docker.Removes);
        Assert.True(docker.ListCalls >= 2);
    }

    [Fact]
    public async Task Exec_is_enabled_only_for_running_container_and_launches()
    {
        var exec = new FakeExecService(ContainerShell.Bash);
        var vm = new MainViewModel(new FakeWslService(), new FakeDockerClient(), new WslDockOptions(), new Confirmation(true), exec);
        await vm.InitializeAsync();
        Assert.False(vm.ExecCommand.CanExecute(null));
        vm.SelectedContainer = vm.Containers[0];
        Assert.True(vm.ExecCommand.CanExecute(null));
        await vm.ExecCommand.ExecuteAsync(null);
        Assert.Equal(("Ubuntu", "id", ContainerShell.Bash), exec.Launched);
        Assert.False(vm.IsBusy);
        vm.SelectedContainer = vm.Containers[0] with { State = DockerContainerState.Paused };
        Assert.False(vm.ExecCommand.CanExecute(null));
    }

    [Fact]
    public async Task Exec_without_shell_sets_readable_error_and_does_not_launch()
    {
        var exec = new FakeExecService(null);
        var vm = new MainViewModel(new FakeWslService(), new FakeDockerClient(), new WslDockOptions(), new Confirmation(true), exec);
        await vm.InitializeAsync(); vm.SelectedContainer = vm.Containers[0];
        await vm.ExecCommand.ExecuteAsync(null);
        Assert.Contains("/bin/bash", vm.ErrorMessage);
        Assert.Null(exec.Launched);
    }

    private sealed class FakeWslService : IWslService
    {
        public Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WslDistribution>>([new("Ubuntu"), new("Debian")]);
    }

    private sealed class FakeDockerClient : IDockerClient
    {
        public int Starts { get; private set; }
        public int ListCalls { get; private set; }
        public int Removes { get; private set; }
        public Task<DockerEngineStatus> GetStatusAsync(string distribution, CancellationToken cancellationToken = default) => Task.FromResult(DockerEngineStatus.Running);
        public Task<IReadOnlyList<DockerContainer>> GetContainersAsync(string distribution, CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<DockerContainer>>([new("id", "web", "nginx", DockerContainerState.Running, "Up", "80/tcp")]);
        }
        public Task StartAsync(string distribution, string containerId, CancellationToken cancellationToken = default) { Starts++; return Task.CompletedTask; }
        public Task StopAsync(string distribution, string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RestartAsync(string distribution, string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string distribution, string containerId, CancellationToken cancellationToken = default) { Removes++; return Task.CompletedTask; }
        public Task<string> GetLogsAsync(string distribution, string containerId, int tailLines, CancellationToken cancellationToken = default) => Task.FromResult("logs");
    }
    private sealed class Confirmation(bool answer) : IConfirmationService { public bool Confirm(string message) => answer; }
    private sealed class FakeExecService(ContainerShell? shell) : IContainerExecService
    {
        public (string Distribution, string Id, ContainerShell Shell)? Launched { get; private set; }
        public Task<ContainerShell?> DetectShellAsync(string distribution, string containerId, CancellationToken cancellationToken = default) => Task.FromResult(shell);
        public Task OpenInteractiveShellAsync(string distribution, string containerId, ContainerShell selectedShell, CancellationToken cancellationToken = default) { Launched = (distribution, containerId, selectedShell); return Task.CompletedTask; }
    }
}
