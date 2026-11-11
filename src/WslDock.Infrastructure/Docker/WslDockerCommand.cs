using WslDock.Application;
using WslDock.Infrastructure.Process;

namespace WslDock.Infrastructure.Docker;

internal sealed class WslDockerCommand(IProcessRunner runner)
{
    public Task<ProcessResult> RunAsync(string distribution, IReadOnlyList<string> arguments, CancellationToken token) =>
        runner.RunAsync(CreateRequest(distribution, arguments), token);

    public async Task<ProcessResult> RunAsync(string distribution, IReadOnlyList<string> arguments, string operation, CancellationToken token)
    {
        var result = await RunAsync(distribution, arguments, token);
        if (!result.IsSuccess)
            throw new DockerCommandException($"Docker could not {operation}. {result.StandardError.Trim()}");
        return result;
    }

    public static ProcessRequest CreateRequest(string distribution, IReadOnlyList<string> arguments) =>
        new("wsl.exe", ["-d", distribution, "--", "docker", .. arguments]);

    public static string CombineLogs(ProcessResult result) =>
        string.Join('\n', new[] { result.StandardError.TrimEnd(), result.StandardOutput.TrimEnd() }.Where(line => line.Length > 0));
}
