namespace WslDock.Application.Processes;
public interface ITerminalLauncher
{
    Task LaunchAsync(TerminalLaunchRequest request, CancellationToken cancellationToken = default);
}
