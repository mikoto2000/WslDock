using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Application.Processes;
using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public sealed class WslContainerExecService(IProcessRunner runner, ITerminalLauncher terminalLauncher, ILogger<WslContainerExecService> logger) : IContainerExecService
{
    public async Task<ContainerShell?> DetectShellAsync(string distribution, string containerId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Detecting shell for container {ContainerId} in {Distribution}", containerId, distribution);
        if (await ExistsAsync(distribution, containerId, "/bin/bash", cancellationToken)) return Detected(ContainerShell.Bash, containerId);
        if (await ExistsAsync(distribution, containerId, "/bin/sh", cancellationToken)) return Detected(ContainerShell.Sh, containerId);
        logger.LogWarning("No supported shell detected for container {ContainerId}", containerId);
        return null;
    }

    public async Task OpenInteractiveShellAsync(string distribution, string containerId, ContainerShell shell, CancellationToken cancellationToken = default)
    {
        var path = shell == ContainerShell.Bash ? "/bin/bash" : "/bin/sh";
        logger.LogInformation("Launching terminal for container {ContainerId} in {Distribution} with {Shell}", containerId, distribution, path);
        await terminalLauncher.LaunchAsync(new TerminalLaunchRequest("wsl.exe", ["-d", distribution, "--", "docker", "exec", "-it", containerId, path]), cancellationToken);
        logger.LogInformation("Terminal launch succeeded for container {ContainerId}", containerId);
    }

    private async Task<bool> ExistsAsync(string distribution, string containerId, string path, CancellationToken token)
    {
        var request = new ProcessRequest("wsl.exe", "-d", distribution, "--", "docker", "exec", containerId, "test", "-x", path);
        var result = await runner.RunAsync(request, token);
        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardError))
            throw new DockerCommandException($"Docker could not detect a container shell. {result.StandardError.Trim()}");
        return result.IsSuccess;
    }

    private ContainerShell Detected(ContainerShell shell, string id) { logger.LogInformation("Detected {Shell} for container {ContainerId}", shell, id); return shell; }
}
