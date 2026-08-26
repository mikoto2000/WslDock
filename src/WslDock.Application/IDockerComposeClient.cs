using WslDock.Domain;
namespace WslDock.Application;
public interface IDockerComposeClient
{
    Task<bool> IsAvailableAsync(string distribution, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DockerComposeProject>> GetProjectsAsync(string distribution, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DockerComposeService>> GetServicesAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default);
    Task UpAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default);
    Task DownAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default);
    Task RestartAsync(string distribution, DockerComposeProject project, CancellationToken cancellationToken = default);
    Task<string> GetLogsAsync(string distribution, DockerComposeProject project, int tailLines, CancellationToken cancellationToken = default);
}
