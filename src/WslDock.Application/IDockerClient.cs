using WslDock.Domain;

namespace WslDock.Application;

public interface IDockerClient
{
    Task<DockerEngineStatus> GetStatusAsync(string distribution, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DockerContainer>> GetContainersAsync(string distribution, CancellationToken cancellationToken = default);
    Task StartAsync(string distribution, string containerId, CancellationToken cancellationToken = default);
    Task StopAsync(string distribution, string containerId, CancellationToken cancellationToken = default);
    Task RestartAsync(string distribution, string containerId, CancellationToken cancellationToken = default);
    Task<string> GetLogsAsync(string distribution, string containerId, int tailLines, CancellationToken cancellationToken = default);
}
