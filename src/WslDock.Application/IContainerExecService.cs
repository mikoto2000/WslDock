namespace WslDock.Application;

public interface IContainerExecService
{
    Task OpenInteractiveShellAsync(string distribution, string containerId, CancellationToken cancellationToken = default);
}
