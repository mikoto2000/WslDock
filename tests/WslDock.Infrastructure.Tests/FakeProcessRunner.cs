using WslDock.Infrastructure.Process;

namespace WslDock.Infrastructure.Tests;

internal sealed class FakeProcessRunner(ProcessResult result) : IProcessRunner
{
    public List<ProcessRequest> Requests { get; } = [];

    public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(result);
    }
}
