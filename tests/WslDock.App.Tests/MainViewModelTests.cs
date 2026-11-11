using WslDock.App.ViewModels;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task Initialize_selects_configured_distribution_and_loads_containers()
    {
        var docker = new FakeDockerClient();
        using var vm = CreateMain(docker, new WslDockOptions { DefaultDistribution = "Debian" });
        await vm.InitializeAsync();
        Assert.Equal("Debian", vm.SelectedDistribution?.Name);
        Assert.Single(vm.ContainersViewModel.Containers);
        Assert.Equal(DockerEngineStatus.Running, vm.ContainersViewModel.EngineStatus);
    }

    [Fact]
    public async Task Start_refreshes_list_after_success()
    {
        var docker = new FakeDockerClient();
        using var vm = CreateContainers(docker);
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SelectedContainer = vm.Containers[0] with { State = DockerContainerState.Exited };
        await vm.StartCommand.ExecuteAsync(null);
        Assert.Equal(1, docker.Starts);
        Assert.True(docker.ListCalls >= 2);
    }

    [Fact]
    public async Task Running_container_disables_start_and_enables_stop()
    {
        using var vm = CreateContainers(new FakeDockerClient());
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SelectedContainer = vm.Containers[0];
        Assert.False(vm.StartCommand.CanExecute(null));
        Assert.True(vm.StopCommand.CanExecute(null));
        Assert.False(vm.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Remove_requires_confirmation_and_refreshes()
    {
        var docker = new FakeDockerClient();
        using var vm = CreateContainers(docker);
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SelectedContainer = vm.Containers[0] with { State = DockerContainerState.Exited };
        await vm.RemoveCommand.ExecuteAsync(null);
        Assert.Equal(1, docker.Removes);
        Assert.True(docker.ListCalls >= 2);
    }

    [Fact]
    public async Task Exec_is_enabled_only_for_running_container_and_launches()
    {
        var exec = new FakeExecService();
        using var vm = CreateContainers(new FakeDockerClient(), exec);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.ExecCommand.CanExecute(null));
        vm.SelectedContainer = vm.Containers[0];
        Assert.True(vm.ExecCommand.CanExecute(null));
        await vm.ExecCommand.ExecuteAsync(null);
        Assert.Equal(("Ubuntu", "id"), exec.Launched);
        Assert.False(vm.IsBusy);
        vm.SelectedContainer = vm.Containers[0] with { State = DockerContainerState.Paused };
        Assert.False(vm.ExecCommand.CanExecute(null));
    }

    [Fact]
    public async Task Exec_without_shell_sets_readable_error_and_does_not_launch()
    {
        var exec = new FakeExecService(missingShell: true);
        using var vm = CreateContainers(new FakeDockerClient(), exec);
        await vm.RefreshCommand.ExecuteAsync(null); vm.SelectedContainer = vm.Containers[0];
        await vm.ExecCommand.ExecuteAsync(null);
        Assert.Contains("/bin/bash", vm.ErrorMessage);
        Assert.Null(exec.Launched);
    }

    [Fact]
    public async Task Distribution_switch_discards_old_container_results_and_clears_logs()
    {
        var docker = new FakeDockerClient();
        using var vm = CreateMain(docker, new WslDockOptions());
        await vm.InitializeAsync();
        var containers = vm.ContainersViewModel;
        using var disposeContainers = containers;
        containers.SelectedContainer = containers.Containers[0];
        await containers.LoadLogsCommand.ExecuteAsync(null);
        var oldList = new TaskCompletionSource<IReadOnlyList<DockerContainer>>();
        docker.Load = distribution => distribution == "Ubuntu"
            ? oldList.Task
            : Task.FromResult<IReadOnlyList<DockerContainer>>([new("debian", "web", "nginx", DockerContainerState.Running, "Up", "")]);

        var staleRefresh = containers.RefreshCommand.ExecuteAsync(null);
        vm.SelectedDistribution = vm.Distributions[1];
        oldList.SetResult([new("ubuntu", "web", "nginx", DockerContainerState.Running, "Up", "")]);
        await staleRefresh;

        Assert.Equal("debian", Assert.Single(containers.Containers).Id);
        Assert.Null(containers.Logs);
        Assert.False(containers.IsBusy);
    }

    private static ContainersViewModel CreateContainers(FakeDockerClient client, FakeExecService? exec = null) =>
        new(new DistributionContext { SelectedDistribution = new("Ubuntu") }, client, new WslDockOptions(), new Confirmation(true), exec ?? new());

    private static MainViewModel CreateMain(FakeDockerClient client, WslDockOptions options)
    {
        var context = new DistributionContext();
        // The shell only composes these tabs. This test exercises the production selection and Containers path.
        return new MainViewModel(new FakeWslService(), options, context,
            new ContainersViewModel(context, client, options, new Confirmation(true), new FakeExecService()),
            null!, null!, null!, null!);
    }

    private sealed class FakeWslService : IWslService
    {
        public Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WslDistribution>>([new("Ubuntu"), new("Debian")]);
    }

    private sealed class FakeDockerClient : IDockerClient
    {
        public Func<string, Task<IReadOnlyList<DockerContainer>>>? Load { get; set; }
        public int Starts { get; private set; }
        public int ListCalls { get; private set; }
        public int Removes { get; private set; }
        public Task<DockerEngineStatus> GetStatusAsync(string distribution, CancellationToken cancellationToken = default) => Task.FromResult(DockerEngineStatus.Running);
        public Task<IReadOnlyList<DockerContainer>> GetContainersAsync(string distribution, CancellationToken cancellationToken = default)
        {
            ListCalls++;
            if (Load is not null) return Load(distribution);
            return Task.FromResult<IReadOnlyList<DockerContainer>>([new("id", "web", "nginx", DockerContainerState.Running, "Up", "80/tcp")]);
        }
        public Task StartAsync(string distribution, string containerId, CancellationToken cancellationToken = default) { Starts++; return Task.CompletedTask; }
        public Task StopAsync(string distribution, string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RestartAsync(string distribution, string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string distribution, string containerId, CancellationToken cancellationToken = default) { Removes++; return Task.CompletedTask; }
        public Task<string> GetLogsAsync(string distribution, string containerId, int tailLines, CancellationToken cancellationToken = default) => Task.FromResult("logs");
    }
    private sealed class Confirmation(bool answer) : IConfirmationService { public bool Confirm(string message) => answer; }
    private sealed class FakeExecService(bool missingShell = false) : IContainerExecService
    {
        public (string Distribution, string Id)? Launched { get; private set; }

        public Task OpenInteractiveShellAsync(string distribution, string containerId, CancellationToken cancellationToken = default)
        {
            if (missingShell) throw new DockerCommandException("Missing /bin/bash or /bin/sh.");
            Launched = (distribution, containerId);
            return Task.CompletedTask;
        }
    }
}
