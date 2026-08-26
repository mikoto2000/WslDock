using WslDock.Application;
using WslDock.Application.Processes;
using WslDock.Domain;

namespace WslDock.Infrastructure.Wsl;

public sealed class WslService(IProcessRunner processRunner) : IWslService
{
    public async Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default)
    {
        ProcessResult result;
        try
        {
            result = await processRunner.RunAsync(new ProcessRequest("wsl.exe", "--list", "--quiet"), cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException("WSL is not available on this computer.", exception);
        }

        if (!result.IsSuccess)
            throw new InvalidOperationException($"WSL distributions could not be loaded. {result.StandardError.Trim()}");
        return WslDistributionParser.Parse(result.StandardOutput);
    }
}
