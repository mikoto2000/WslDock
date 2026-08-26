using WslDock.App.Services;
using WslDock.App.ViewModels;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.Application.Tests;

public sealed class ResourceViewModelTests
{
    private static DistributionContext Context() => new() { SelectedDistribution = new("Ubuntu") };

    [Fact]
    public async Task Image_pull_refreshes_after_success()
    {
        var client = new ImageClient(); var vm = new ImagesViewModel(Context(), client, new Confirmation(true)) { ImageReference = "nginx:latest" };
        await vm.PullCommand.ExecuteAsync(null);
        Assert.Equal("nginx:latest", client.Pulled);
        Assert.Single(vm.Images);
    }

    [Fact]
    public void Standard_network_cannot_be_removed()
    {
        var vm = new NetworksViewModel(Context(), new NetworkClient(), new Confirmation(true)) { SelectedNetwork = new("id", "bridge", "bridge", "local") };
        Assert.False(vm.RemoveCommand.CanExecute(null));
        vm.SelectedNetwork = new("id2", "custom", "bridge", "local");
        Assert.True(vm.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Declined_volume_confirmation_does_not_remove()
    {
        var client = new VolumeClient(); var vm = new VolumesViewModel(Context(), client, new Confirmation(false)) { SelectedVolume = new("data", "local", "local") };
        await vm.RemoveCommand.ExecuteAsync(null);
        Assert.False(client.Removed);
    }

    private sealed class Confirmation(bool answer) : IConfirmationService { public bool Confirm(string message) => answer; }
    private sealed class ImageClient : IDockerImageClient
    {
        public string? Pulled { get; private set; }
        public Task<IReadOnlyList<DockerImage>> GetImagesAsync(string d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DockerImage>>([new("id", "nginx", "latest", "digest", "now", "1MB")]);
        public Task PullAsync(string d, string r, CancellationToken ct = default) { Pulled = r; return Task.CompletedTask; }
        public Task RemoveAsync(string d, string id, CancellationToken ct = default) => Task.CompletedTask;
        public Task PruneAsync(string d, CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class NetworkClient : IDockerNetworkClient
    {
        public Task<IReadOnlyList<DockerNetwork>> GetNetworksAsync(string d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DockerNetwork>>([]);
        public Task RemoveAsync(string d, string id, CancellationToken ct = default) => Task.CompletedTask;
        public Task PruneAsync(string d, CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class VolumeClient : IDockerVolumeClient
    {
        public bool Removed { get; private set; }
        public Task<IReadOnlyList<DockerVolume>> GetVolumesAsync(string d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DockerVolume>>([]);
        public Task RemoveAsync(string d, string id, CancellationToken ct = default) { Removed = true; return Task.CompletedTask; }
        public Task PruneAsync(string d, CancellationToken ct = default) => Task.CompletedTask;
    }
}
