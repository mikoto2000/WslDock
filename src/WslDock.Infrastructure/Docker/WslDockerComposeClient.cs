using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Application.Processes;
using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public sealed class WslDockerComposeClient(IProcessRunner runner, ILogger<WslDockerComposeClient> logger) : IDockerComposeClient
{
    private WslDockerCommand Command => new(runner);
    public async Task<bool> IsAvailableAsync(string d, CancellationToken ct = default)
    {
        var args = new ProcessRequest("wsl.exe", "-d", d, "--", "docker", "compose", "version");
        try { return (await runner.RunAsync(args, ct)).IsSuccess; } catch (InvalidOperationException) { return false; }
    }
    public async Task<IReadOnlyList<DockerComposeProject>> GetProjectsAsync(string d, CancellationToken ct = default) => DockerComposeParser.ParseProjects((await Command.RunAsync(d, ["compose", "ls", "--all", "--format", "json"], "list Compose projects", ct)).StandardOutput);
    public async Task<IReadOnlyList<DockerComposeService>> GetServicesAsync(string d, DockerComposeProject p, CancellationToken ct = default) => DockerComposeParser.ParseServices((await Command.RunAsync(d, DockerComposeCommandBuilder.Build(p, ["ps", "-a", "--format", "json"]), "list Compose services", ct)).StandardOutput);
    public Task UpAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Run(d, p, ["up", "-d"], "Compose up", ct);
    public Task DownAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Run(d, p, ["down"], "Compose down", ct);
    public Task RestartAsync(string d, DockerComposeProject p, CancellationToken ct = default) => Run(d, p, ["restart"], "Compose restart", ct);
    public async Task<string> GetLogsAsync(string d, DockerComposeProject p, int lines, CancellationToken ct = default)
    {
        var result = await Command.RunAsync(d, DockerComposeCommandBuilder.Build(p, ["logs", "--tail", Math.Max(1, lines).ToString(System.Globalization.CultureInfo.InvariantCulture)]), "load Compose logs", ct);
        return string.Join('\n', new[] { result.StandardError.TrimEnd(), result.StandardOutput.TrimEnd() }.Where(x => x.Length > 0));
    }
    private async Task Run(string d, DockerComposeProject p, IReadOnlyList<string> opArgs, string op, CancellationToken ct) { logger.LogInformation("Docker {Operation} project {Project}", op, p.Name); await Command.RunAsync(d, DockerComposeCommandBuilder.Build(p, opArgs), op, ct); }
}
