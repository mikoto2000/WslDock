using WslDock.Infrastructure.Process;
using Microsoft.Extensions.Logging.Abstractions;
using WslDock.Domain;
using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class WslDockerCliClientTests
{
    [Theory]
    [InlineData("start")]
    [InlineData("stop")]
    [InlineData("restart")]
    [InlineData("rm")]
    public async Task Operation_passes_distribution_and_id_as_separate_arguments(string operation)
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "abc", ""));
        var client = new WslDockerCliClient(runner, NullLogger<WslDockerCliClient>.Instance);

        await Invoke(client, operation);

        Assert.Equal(["-d", "Ubuntu Dev", "--", "docker", operation, "abc;rm -rf"], Assert.Single(runner.Requests).Arguments);
    }

    [Fact]
    public async Task List_uses_json_lines_and_parses_result()
    {
        var output = "{\"ID\":\"1\",\"Names\":\"web\",\"Image\":\"nginx\",\"State\":\"running\",\"Status\":\"Up\",\"Ports\":\"80/tcp\"}";
        var runner = new FakeProcessRunner(new ProcessResult(0, output, ""));
        var client = new WslDockerCliClient(runner, NullLogger<WslDockerCliClient>.Instance);

        var item = Assert.Single(await client.GetContainersAsync("Ubuntu"));

        Assert.Equal(DockerContainerState.Running, item.State);
        Assert.Contains("{{json .}}", Assert.Single(runner.Requests).Arguments);
    }

    [Fact]
    public async Task Logs_uses_configured_tail()
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "logs", "warnings"));
        var client = new WslDockerCliClient(runner, NullLogger<WslDockerCliClient>.Instance);
        var result = await client.GetLogsAsync("Ubuntu", "abc", 500);
        Assert.Equal("warnings\nlogs", result);
        Assert.Equal(["-d", "Ubuntu", "--", "docker", "logs", "--tail", "500", "abc"], Assert.Single(runner.Requests).Arguments);
    }

    [Theory]
    [InlineData(0, "", DockerEngineStatus.Running)]
    [InlineData(127, "docker: not found", DockerEngineStatus.CliNotInstalled)]
    [InlineData(1, "Cannot connect to the Docker daemon", DockerEngineStatus.DaemonUnavailable)]
    [InlineData(1, "other", DockerEngineStatus.Unavailable)]
    public async Task Status_classifies_result(int exitCode, string stderr, DockerEngineStatus expected)
    {
        var client = new WslDockerCliClient(new FakeProcessRunner(new ProcessResult(exitCode, "", stderr)), NullLogger<WslDockerCliClient>.Instance);
        Assert.Equal(expected, await client.GetStatusAsync("Ubuntu"));
    }

    private static Task Invoke(WslDockerCliClient client, string operation) => operation switch
    {
        "start" => client.StartAsync("Ubuntu Dev", "abc;rm -rf"),
        "stop" => client.StopAsync("Ubuntu Dev", "abc;rm -rf"),
        "restart" => client.RestartAsync("Ubuntu Dev", "abc;rm -rf"),
        _ => client.RemoveAsync("Ubuntu Dev", "abc;rm -rf")
    };
}
