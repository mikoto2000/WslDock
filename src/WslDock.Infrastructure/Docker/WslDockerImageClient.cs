using Microsoft.Extensions.Logging;
using WslDock.Application;
using WslDock.Application.Processes;
using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public sealed class WslDockerImageClient(IProcessRunner runner, ILogger<WslDockerImageClient> logger) : IDockerImageClient
{
    private WslDockerCommand Command => new(runner);
    public async Task<IReadOnlyList<DockerImage>> GetImagesAsync(string d, CancellationToken ct = default) => DockerImageParser.Parse((await Command.RunAsync(d, ["image", "ls", "--no-trunc", "--format", "{{json .}}"], "list images", ct)).StandardOutput);
    public Task PullAsync(string d, string r, CancellationToken ct = default) => Run(d, ["pull", r], "pull image", r, ct);
    public Task RemoveAsync(string d, string id, CancellationToken ct = default) => Run(d, ["image", "rm", id], "remove image", id, ct);
    public Task PruneAsync(string d, CancellationToken ct = default) => Run(d, ["image", "prune", "-f"], "prune images", "unused", ct);
    private async Task Run(string d, IReadOnlyList<string> args, string op, string id, CancellationToken ct) { logger.LogInformation("Docker {Operation} {Identifier}", op, id); await Command.RunAsync(d, args, op, ct); }
}
