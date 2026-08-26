using WslDock.Application;
using WslDock.Application.Processes;
namespace WslDock.Infrastructure.Docker;
internal sealed class WslDockerCommand(IProcessRunner runner)
{
    public async Task<ProcessResult> RunAsync(string distribution, IReadOnlyList<string> command, string operation, CancellationToken token)
    {
        var args = new List<string> { "-d", distribution, "--", "docker" }; args.AddRange(command);
        var result = await runner.RunAsync(new ProcessRequest("wsl.exe", args), token);
        if (!result.IsSuccess) throw new DockerCommandException($"Docker could not {operation}. {result.StandardError.Trim()}");
        return result;
    }
}
