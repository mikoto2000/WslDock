using System.Globalization;
using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Domain;
using WslDock.Infrastructure.Process;

namespace WslDock.Infrastructure.Docker;

public sealed class WslDockerCliClient(IProcessRunner processRunner, ILogger<WslDockerCliClient> logger) : IDockerClient
{
    private readonly WslDockerCommand command = new(processRunner);

    public async Task<DockerEngineStatus> GetStatusAsync(string distribution, CancellationToken cancellationToken = default)
    {
        var result = await command.RunAsync(distribution, ["info", "--format", "{{json .ServerVersion}}"], cancellationToken);
        if (result.IsSuccess) return DockerEngineStatus.Running;
        var error = result.StandardError;
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase) || result.ExitCode == 127) return DockerEngineStatus.CliNotInstalled;
        if (error.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase) || error.Contains("daemon", StringComparison.OrdinalIgnoreCase)) return DockerEngineStatus.DaemonUnavailable;
        return DockerEngineStatus.Unavailable;
    }

    public async Task<IReadOnlyList<DockerContainer>> GetContainersAsync(string distribution, CancellationToken cancellationToken = default)
    {
        var result = await command.RunAsync(distribution, ["ps", "-a", "--format", "{{json .}}"], "load containers", cancellationToken);
        return DockerContainerParser.Parse(result.StandardOutput);
    }

    public Task StartAsync(string distribution, string containerId, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, "start", containerId, cancellationToken);

    public Task StopAsync(string distribution, string containerId, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, "stop", containerId, cancellationToken);

    public Task RestartAsync(string distribution, string containerId, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, "restart", containerId, cancellationToken);

    public Task RemoveAsync(string distribution, string containerId, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, "rm", containerId, cancellationToken);

    public async Task<string> GetLogsAsync(string distribution, string containerId, int tailLines, CancellationToken cancellationToken = default)
    {
        var lines = Math.Max(1, tailLines).ToString(CultureInfo.InvariantCulture);
        var result = await command.RunAsync(distribution, ["logs", "--tail", lines, containerId], "load logs", cancellationToken);
        return WslDockerCommand.CombineLogs(result);
    }

    private async Task ExecuteOperationAsync(string distribution, string operation, string containerId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Docker {Operation} requested for container {ContainerId}", operation, containerId);
        await command.RunAsync(distribution, [operation, containerId], operation, cancellationToken);
    }
}
