using System.Globalization;
using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Domain;
using WslDock.Infrastructure.Process;

namespace WslDock.Infrastructure.Docker;

public sealed class WslDockerComposeClient(IProcessRunner runner, ILogger<WslDockerComposeClient> logger) : IDockerComposeClient
{
    private readonly WslDockerCommand command = new(runner);

    public async Task<bool> IsAvailableAsync(string distribution, CancellationToken cancellationToken = default)
    {
        try
        {
            return (await command.RunAsync(distribution, ["compose", "version"], cancellationToken)).IsSuccess;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<DockerComposeProject>> GetProjectsAsync(string distribution, CancellationToken cancellationToken = default)
    {
        var result = await command.RunAsync(distribution, ["compose", "ls", "--all", "--format", "json"], "list Compose projects", cancellationToken);
        return DockerComposeParser.ParseProjects(result.StandardOutput);
    }

    public async Task<IReadOnlyList<DockerComposeService>> GetServicesAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default)
    {
        var arguments = DockerComposeCommandBuilder.Build(project, ["ps", "-a", "--format", "json"]);
        var result = await command.RunAsync(distribution, arguments, "list Compose services", cancellationToken);
        return DockerComposeParser.ParseServices(result.StandardOutput);
    }

    public Task UpAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, project, ["up", "-d"], "Compose up", cancellationToken);

    public Task DownAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, project, ["down"], "Compose down", cancellationToken);

    public Task RestartAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default) =>
        ExecuteOperationAsync(distribution, project, ["restart"], "Compose restart", cancellationToken);

    public async Task<string> GetLogsAsync(string distribution, DockerComposeProject project, int tailLines, CancellationToken cancellationToken = default)
    {
        var lines = Math.Max(1, tailLines).ToString(CultureInfo.InvariantCulture);
        var arguments = DockerComposeCommandBuilder.Build(project, ["logs", "--tail", lines]);
        var result = await command.RunAsync(distribution, arguments, "load Compose logs", cancellationToken);
        return WslDockerCommand.CombineLogs(result);
    }

    private async Task ExecuteOperationAsync(string distribution, DockerComposeProject project, IReadOnlyList<string> arguments, string operation, CancellationToken token)
    {
        logger.LogInformation("Docker {Operation} project {Project}", operation, project.Name);
        await command.RunAsync(distribution, DockerComposeCommandBuilder.Build(project, arguments), operation, token);
    }
}
