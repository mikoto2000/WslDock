namespace WslDock.Infrastructure.Process;
public interface ITerminalLauncher
{
    Task LaunchAsync(TerminalLaunchRequest request, CancellationToken cancellationToken = default);
}
