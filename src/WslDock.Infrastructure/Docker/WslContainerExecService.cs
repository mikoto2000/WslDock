using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Infrastructure.Process;

namespace WslDock.Infrastructure.Docker;

public sealed class WslContainerExecService(
    IProcessRunner runner,
    ITerminalLauncher terminalLauncher,
    ILogger<WslContainerExecService> logger) : IContainerExecService
{
    private readonly WslDockerCommand command = new(runner);

    public async Task OpenInteractiveShellAsync(string distribution, string containerId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Detecting shell for container {ContainerId} in {Distribution}", containerId, distribution);
        foreach (var path in new[] { "/bin/bash", "/bin/sh" })
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await command.RunAsync(distribution, ["exec", containerId, "test", "-x", path], cancellationToken);
            if (!result.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(result.StandardError))
                    throw new DockerCommandException($"Docker could not detect a container shell. {result.StandardError.Trim()}");
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var request = WslDockerCommand.CreateRequest(distribution, ["exec", "-it", containerId, path]);
            logger.LogInformation("Launching terminal for container {ContainerId} in {Distribution} with {Shell}", containerId, distribution, path);
            await terminalLauncher.LaunchAsync(new TerminalLaunchRequest(request.Executable, request.Arguments), cancellationToken);
            return;
        }

        throw new DockerCommandException("This container does not appear to provide /bin/bash or /bin/sh.");
    }
}
