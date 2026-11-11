using Microsoft.Extensions.Logging.Abstractions;
using WslDock.Application;
using WslDock.Infrastructure.Docker;
using WslDock.Infrastructure.Process;

namespace WslDock.Infrastructure.Tests;

public sealed class WslContainerExecServiceTests
{
    [Theory]
    [InlineData(false, "/bin/bash")]
    [InlineData(true, "/bin/sh")]
    public async Task Exec_probes_shells_and_launches_with_structured_arguments(bool fallBack, string path)
    {
        var runner = fallBack
            ? new SequenceRunner(new ProcessResult(1, "", ""), new ProcessResult(0, "", ""))
            : new SequenceRunner(new ProcessResult(0, "", ""));
        var launcher = new FakeTerminalLauncher();
        var service = new WslContainerExecService(runner, launcher, NullLogger<WslContainerExecService>.Instance);

        await service.OpenInteractiveShellAsync("Ubuntu Dev", "abc;echo bad");

        Assert.Equal(fallBack ? new[] { "/bin/bash", "/bin/sh" } : ["/bin/bash"], runner.Requests.Select(request => request.Arguments[^1]));
        Assert.Equal(["-d", "Ubuntu Dev", "--", "docker", "exec", "abc;echo bad", "test", "-x", "/bin/bash"], runner.Requests[0].Arguments);
        Assert.Equal("wsl.exe", launcher.Request!.FileName);
        Assert.Equal(["-d", "Ubuntu Dev", "--", "docker", "exec", "-it", "abc;echo bad", path], launcher.Request.Arguments);
    }

    [Fact]
    public async Task Exec_reports_missing_shells_without_launching()
    {
        var runner = new SequenceRunner(new ProcessResult(1, "", ""), new ProcessResult(1, "", ""));
        var launcher = new FakeTerminalLauncher();
        var service = new WslContainerExecService(runner, launcher, NullLogger<WslContainerExecService>.Instance);

        var error = await Assert.ThrowsAsync<DockerCommandException>(() => service.OpenInteractiveShellAsync("Ubuntu", "abc"));

        Assert.Contains("/bin/bash or /bin/sh", error.Message);
        Assert.Null(launcher.Request);
    }

    [Theory]
    [InlineData("container is not running")]
    [InlineData("Cannot connect to the Docker daemon")]
    public async Task Exec_reports_Docker_failures(string error)
    {
        var launcher = new FakeTerminalLauncher();
        var service = new WslContainerExecService(new SequenceRunner(new ProcessResult(1, "", error)), launcher, NullLogger<WslContainerExecService>.Instance);

        var exception = await Assert.ThrowsAsync<DockerCommandException>(() => service.OpenInteractiveShellAsync("Ubuntu", "abc"));

        Assert.Contains(error, exception.Message);
        Assert.Null(launcher.Request);
    }

    [Fact]
    public async Task Cancellation_during_probe_prevents_terminal_launch()
    {
        using var cancellation = new CancellationTokenSource();
        var runner = new SequenceRunner(new ProcessResult(0, "", "")) { BeforeReturn = cancellation.Cancel };
        var launcher = new FakeTerminalLauncher();
        var service = new WslContainerExecService(runner, launcher, NullLogger<WslContainerExecService>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.OpenInteractiveShellAsync("Ubuntu", "abc", cancellation.Token));

        Assert.Null(launcher.Request);
    }

    private sealed class SequenceRunner(params ProcessResult[] results) : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];
        public Action? BeforeReturn { get; init; }

        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            BeforeReturn?.Invoke();
            return Task.FromResult(results[Requests.Count - 1]);
        }
    }

    private sealed class FakeTerminalLauncher : ITerminalLauncher
    {
        public TerminalLaunchRequest? Request { get; private set; }

        public Task LaunchAsync(TerminalLaunchRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.CompletedTask;
        }
    }
}
