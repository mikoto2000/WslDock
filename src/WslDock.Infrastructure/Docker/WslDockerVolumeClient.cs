using WslDock.Infrastructure.Process;
using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public sealed class WslDockerVolumeClient(IProcessRunner runner, ILogger<WslDockerVolumeClient> logger) : IDockerVolumeClient
{
    private readonly WslDockerCommand command = new(runner);
    public async Task<IReadOnlyList<DockerVolume>> GetVolumesAsync(string d, CancellationToken ct = default) => DockerVolumeParser.Parse((await command.RunAsync(d, ["volume", "ls", "--format", "{{json .}}"], "list volumes", ct)).StandardOutput);
    public Task RemoveAsync(string d, string name, CancellationToken ct = default) => Run(d, ["volume", "rm", name], "remove volume", name, ct);
    public Task PruneAsync(string d, CancellationToken ct = default) => Run(d, ["volume", "prune", "-f"], "prune volumes", "unused", ct);
    private async Task Run(string d, IReadOnlyList<string> args, string op, string id, CancellationToken ct) { logger.LogInformation("Docker {Operation} {Identifier}", op, id); await command.RunAsync(d, args, op, ct); }
}
