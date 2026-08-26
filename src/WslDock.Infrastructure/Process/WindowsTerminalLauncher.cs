using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WslDock.Application.Processes;
namespace WslDock.Infrastructure.Process;
public sealed class WindowsTerminalLauncher(ILogger<WindowsTerminalLauncher> logger) : ITerminalLauncher
{
    public Task LaunchAsync(TerminalLaunchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new ProcessStartInfo { FileName = request.FileName, UseShellExecute = true, CreateNoWindow = false, WindowStyle = ProcessWindowStyle.Normal };
        foreach (var argument in request.Arguments) info.ArgumentList.Add(argument);
        try
        {
            if (System.Diagnostics.Process.Start(info) is null) throw new InvalidOperationException("The Windows terminal process was not started.");
            return Task.CompletedTask;
        }
        catch (Win32Exception exception)
        {
            logger.LogError(exception, "Terminal launch failed for {FileName}", request.FileName);
            throw new InvalidOperationException("A new Windows terminal could not be opened.", exception);
        }
    }
}
