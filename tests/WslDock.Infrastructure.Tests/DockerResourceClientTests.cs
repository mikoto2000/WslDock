using WslDock.Infrastructure.Process;
using Microsoft.Extensions.Logging.Abstractions;
using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class DockerResourceClientTests
{
    [Fact]
    public async Task Image_commands_use_structured_arguments()
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "", ""));
        var client = new WslDockerImageClient(runner, NullLogger<WslDockerImageClient>.Instance);
        await client.GetImagesAsync("Ubuntu");
        await client.PullAsync("Ubuntu", "nginx:latest;echo bad");
        await client.RemoveAsync("Ubuntu", "sha256:abc");
        await client.PruneAsync("Ubuntu");
        Assert.Equal(["-d", "Ubuntu", "--", "docker", "image", "ls", "--no-trunc", "--format", "{{json .}}"], runner.Requests[0].Arguments);
        Assert.Equal(["docker", "pull", "nginx:latest;echo bad"], runner.Requests[1].Arguments.TakeLast(3));
        Assert.Equal(["docker", "image", "rm", "sha256:abc"], runner.Requests[2].Arguments.TakeLast(4));
        Assert.Equal(["docker", "image", "prune", "-f"], runner.Requests[3].Arguments.TakeLast(4));
    }

    [Fact]
    public async Task Volume_commands_use_structured_arguments()
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "", ""));
        var client = new WslDockerVolumeClient(runner, NullLogger<WslDockerVolumeClient>.Instance);
        await client.GetVolumesAsync("Ubuntu"); await client.RemoveAsync("Ubuntu", "my volume"); await client.PruneAsync("Ubuntu");
        Assert.Equal(["docker", "volume", "ls", "--format", "{{json .}}"], runner.Requests[0].Arguments.TakeLast(5));
        Assert.Equal(["docker", "volume", "rm", "my volume"], runner.Requests[1].Arguments.TakeLast(4));
        Assert.Equal(["docker", "volume", "prune", "-f"], runner.Requests[2].Arguments.TakeLast(4));
    }

    [Fact]
    public async Task Network_commands_use_ids_and_reject_standard_networks()
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "", ""));
        var client = new WslDockerNetworkClient(runner, NullLogger<WslDockerNetworkClient>.Instance);
        await client.GetNetworksAsync("Ubuntu"); await client.RemoveAsync("Ubuntu", "id"); await client.PruneAsync("Ubuntu");
        Assert.Equal(["docker", "network", "ls", "--format", "{{json .}}"], runner.Requests[0].Arguments.TakeLast(5));
        Assert.Equal(["docker", "network", "rm", "id"], runner.Requests[1].Arguments.TakeLast(4));
        Assert.Equal(["docker", "network", "prune", "-f"], runner.Requests[2].Arguments.TakeLast(4));
    }
}
