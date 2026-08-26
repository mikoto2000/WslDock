using WslDock.Domain;
namespace WslDock.Application;
public interface IDockerNetworkClient
{
    Task<IReadOnlyList<DockerNetwork>> GetNetworksAsync(string distribution, CancellationToken cancellationToken = default);
    Task RemoveAsync(string distribution, string networkId, CancellationToken cancellationToken = default);
    Task PruneAsync(string distribution, CancellationToken cancellationToken = default);
}
