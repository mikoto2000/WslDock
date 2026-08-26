using WslDock.Domain;
namespace WslDock.Application;
public interface IDockerVolumeClient
{
    Task<IReadOnlyList<DockerVolume>> GetVolumesAsync(string distribution, CancellationToken cancellationToken = default);
    Task RemoveAsync(string distribution, string volumeName, CancellationToken cancellationToken = default);
    Task PruneAsync(string distribution, CancellationToken cancellationToken = default);
}
