using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Application.Processes;
using WslDock.Domain;

namespace WslDock.Infrastructure.Docker;

public sealed class WslDockerCliClient(IProcessRunner processRunner, ILogger<WslDockerCliClient> logger) : IDockerClient
{
    public async Task<DockerEngineStatus> GetStatusAsync(string distribution, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(distribution, ["docker", "info", "--format", "{{json .ServerVersion}}"], cancellationToken);
        if (result.IsSuccess) return DockerEngineStatus.Running;
        var error = result.StandardError;
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase) || result.ExitCode == 127) return DockerEngineStatus.CliNotInstalled;
        if (error.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase) || error.Contains("daemon", StringComparison.OrdinalIgnoreCase)) return DockerEngineStatus.DaemonUnavailable;
        return DockerEngineStatus.Unavailable;
    }

    public async Task<IReadOnlyList<DockerContainer>> GetContainersAsync(string distribution, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(distribution, ["docker", "ps", "-a", "--format", "{{json .}}"], cancellationToken);
        EnsureSuccess(result, "load containers");
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
        var result = await RunAsync(distribution, ["docker", "logs", "--tail", Math.Max(1, tailLines).ToString(System.Globalization.CultureInfo.InvariantCulture), containerId], cancellationToken);
        EnsureSuccess(result, "load logs");
        return string.Join('\n', new[] { result.StandardError.TrimEnd(), result.StandardOutput.TrimEnd() }.Where(x => x.Length > 0));
    }

    private async Task ExecuteOperationAsync(string distribution, string operation, string containerId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Docker {Operation} requested for container {ContainerId}", operation, containerId);
        var result = await RunAsync(distribution, ["docker", operation, containerId], cancellationToken);
        EnsureSuccess(result, operation);
    }

    private Task<ProcessResult> RunAsync(string distribution, IReadOnlyList<string> dockerArguments, CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "-d", distribution, "--" };
        arguments.AddRange(dockerArguments);
        return processRunner.RunAsync(new ProcessRequest("wsl.exe", arguments), cancellationToken);
    }

    private static void EnsureSuccess(ProcessResult result, string operation)
    {
        if (!result.IsSuccess)
            throw new DockerCommandException($"Docker could not {operation}. {result.StandardError.Trim()}");
    }
}
