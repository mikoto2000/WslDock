using WslDock.Infrastructure.Process;
using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public sealed class WslDockerNetworkClient(IProcessRunner runner, ILogger<WslDockerNetworkClient> logger) : IDockerNetworkClient
{
    private readonly WslDockerCommand command = new(runner);
    public async Task<IReadOnlyList<DockerNetwork>> GetNetworksAsync(string d, CancellationToken ct = default) => DockerNetworkParser.Parse((await command.RunAsync(d, ["network", "ls", "--format", "{{json .}}"], "list networks", ct)).StandardOutput);
    public Task RemoveAsync(string d, string id, CancellationToken ct = default) => Run(d, ["network", "rm", id], "remove network", id, ct);
    public Task PruneAsync(string d, CancellationToken ct = default) => Run(d, ["network", "prune", "-f"], "prune networks", "unused", ct);
    private async Task Run(string d, IReadOnlyList<string> args, string op, string id, CancellationToken ct) { logger.LogInformation("Docker {Operation} {Identifier}", op, id); await command.RunAsync(d, args, op, ct); }
}
