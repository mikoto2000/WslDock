using Microsoft.Extensions.Logging.Abstractions;
using WslDock.Application.Processes;
using WslDock.Domain;
using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class WslDockerComposeClientTests
{
    private static readonly DockerComposeProject Project = new("demo", "running(1)", ["/work/app/compose.yml"], "/work/app");

    [Fact]
    public async Task List_and_details_use_machine_readable_formats()
    {
        var runner = new SequencedProcessRunner(new ProcessResult(0, "[]", ""), new ProcessResult(0, "", ""));
        var client = new WslDockerComposeClient(runner, NullLogger<WslDockerComposeClient>.Instance);
        await client.GetProjectsAsync("Ubuntu"); await client.GetServicesAsync("Ubuntu", Project);
        Assert.Equal(["docker", "compose", "ls", "--all", "--format", "json"], runner.Requests[0].Arguments.TakeLast(6));
        Assert.Equal(["ps", "-a", "--format", "json"], runner.Requests[1].Arguments.TakeLast(4));
    }

    private sealed class SequencedProcessRunner(params ProcessResult[] results) : IProcessRunner
    {
        private int index;
        public List<ProcessRequest> Requests { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default) { Requests.Add(request); return Task.FromResult(results[index++]); }
    }

    [Fact]
    public async Task Operations_build_safe_compose_arguments()
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "", ""));
        var client = new WslDockerComposeClient(runner, NullLogger<WslDockerComposeClient>.Instance);
        await client.UpAsync("Ubuntu", Project); await client.RestartAsync("Ubuntu", Project); await client.GetLogsAsync("Ubuntu", Project, 500); await client.DownAsync("Ubuntu", Project);
        Assert.Equal(["up", "-d"], runner.Requests[0].Arguments.TakeLast(2));
        Assert.Equal("restart", runner.Requests[1].Arguments[^1]);
        Assert.Equal(["logs", "--tail", "500"], runner.Requests[2].Arguments.TakeLast(3));
        Assert.Equal("down", runner.Requests[3].Arguments[^1]);
    }

    [Fact]
    public async Task Availability_is_independent_result()
    {
        var available = new WslDockerComposeClient(new FakeProcessRunner(new ProcessResult(0, "Docker Compose version v5", "")), NullLogger<WslDockerComposeClient>.Instance);
        Assert.True(await available.IsAvailableAsync("Ubuntu"));
        var missing = new WslDockerComposeClient(new FakeProcessRunner(new ProcessResult(1, "", "not found")), NullLogger<WslDockerComposeClient>.Instance);
        Assert.False(await missing.IsAvailableAsync("Ubuntu"));
    }
}
