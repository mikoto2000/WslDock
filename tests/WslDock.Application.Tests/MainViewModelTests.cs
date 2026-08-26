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
}
