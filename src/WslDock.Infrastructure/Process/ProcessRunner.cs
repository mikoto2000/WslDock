using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WslDock.Infrastructure.Process;

public sealed class ProcessRunner(ILogger<ProcessRunner> logger) : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new ProcessStartInfo
        {
            FileName = request.Executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in request.Arguments) info.ArgumentList.Add(argument);

        logger.LogInformation("Starting process {Executable} with {ArgumentCount} arguments", request.Executable, request.Arguments.Count);
        using var process = new System.Diagnostics.Process { StartInfo = info };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            logger.LogError(exception, "Could not start {Executable}", request.Executable);
            throw new InvalidOperationException($"{request.Executable} could not be started.", exception);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }

        var result = new ProcessResult(process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
        logger.LogInformation("Process {Executable} exited with code {ExitCode}", request.Executable, result.ExitCode);
        if (!result.IsSuccess) logger.LogWarning("Process {Executable} failed: {Error}", request.Executable, result.StandardError);
        return result;
    }
}
