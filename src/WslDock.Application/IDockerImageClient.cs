using WslDock.Domain;
namespace WslDock.Application;
public interface IDockerImageClient
{
    Task<IReadOnlyList<DockerImage>> GetImagesAsync(string distribution, CancellationToken cancellationToken = default);
    Task PullAsync(string distribution, string imageReference, CancellationToken cancellationToken = default);
    Task RemoveAsync(string distribution, string imageId, CancellationToken cancellationToken = default);
    Task PruneAsync(string distribution, CancellationToken cancellationToken = default);
}
