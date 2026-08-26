using WslDock.Domain;
namespace WslDock.Application;
public interface IContainerExecService
{
    Task<ContainerShell?> DetectShellAsync(string distribution, string containerId, CancellationToken cancellationToken = default);
    Task OpenInteractiveShellAsync(string distribution, string containerId, ContainerShell shell, CancellationToken cancellationToken = default);
}
