using Microsoft.Extensions.Logging.Abstractions;
using WslDock.Application.Processes;
using WslDock.Domain;
using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class WslContainerExecServiceTests
{
    [Fact]
    public async Task Detect_prefers_bash()
    {
        var runner = new SequenceRunner(new ProcessResult(0, "", ""));
        var service = new WslContainerExecService(runner, new FakeTerminalLauncher(), NullLogger<WslContainerExecService>.Instance);
        Assert.Equal(ContainerShell.Bash, await service.DetectShellAsync("Ubuntu Dev", "abc"));
        Assert.Equal(["-d", "Ubuntu Dev", "--", "docker", "exec", "abc", "test", "-x", "/bin/bash"], runner.Requests[0].Arguments);
    }

    [Fact]
    public async Task Detect_falls_back_to_sh()
    {
        var runner = new SequenceRunner(new ProcessResult(1, "", ""), new ProcessResult(0, "", ""));
        var service = new WslContainerExecService(runner, new FakeTerminalLauncher(), NullLogger<WslContainerExecService>.Instance);
        Assert.Equal(ContainerShell.Sh, await service.DetectShellAsync("Ubuntu", "abc"));
        Assert.Equal("/bin/sh", runner.Requests[1].Arguments[^1]);
    }

    [Fact]
    public async Task Detect_returns_null_when_no_supported_shell_exists()
    {
        var runner = new SequenceRunner(new ProcessResult(1, "", ""), new ProcessResult(1, "", ""));
        var service = new WslContainerExecService(runner, new FakeTerminalLauncher(), NullLogger<WslContainerExecService>.Instance);
        Assert.Null(await service.DetectShellAsync("Ubuntu", "abc"));
    }

    [Theory]
    [InlineData("container is not running")]
    [InlineData("Cannot connect to the Docker daemon")]
    public async Task Detect_reports_Docker_failures(string error)
    {
        var service = new WslContainerExecService(new SequenceRunner(new ProcessResult(1, "", error)), new FakeTerminalLauncher(), NullLogger<WslContainerExecService>.Instance);
        var exception = await Assert.ThrowsAsync<WslDock.Application.DockerCommandException>(() => service.DetectShellAsync("Ubuntu", "abc"));
        Assert.Contains(error, exception.Message);
    }

    [Theory]
    [InlineData(ContainerShell.Bash, "/bin/bash")]
    [InlineData(ContainerShell.Sh, "/bin/sh")]
    public async Task Open_launches_structured_interactive_wsl_command(ContainerShell shell, string path)
    {
        var launcher = new FakeTerminalLauncher();
        var service = new WslContainerExecService(new SequenceRunner(), launcher, NullLogger<WslContainerExecService>.Instance);
        await service.OpenInteractiveShellAsync("Ubuntu Dev", "abc;echo bad", shell);
        Assert.Equal("wsl.exe", launcher.Request!.FileName);
        Assert.Equal(["-d", "Ubuntu Dev", "--", "docker", "exec", "-it", "abc;echo bad", path], launcher.Request.Arguments);
    }

    private sealed class SequenceRunner(params ProcessResult[] results) : IProcessRunner
    {
        private int index;
        public List<ProcessRequest> Requests { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default) { Requests.Add(request); return Task.FromResult(results[index++]); }
    }

    private sealed class FakeTerminalLauncher : ITerminalLauncher
    {
        public TerminalLaunchRequest? Request { get; private set; }
        public Task LaunchAsync(TerminalLaunchRequest request, CancellationToken cancellationToken = default) { Request = request; return Task.CompletedTask; }
    }
}
