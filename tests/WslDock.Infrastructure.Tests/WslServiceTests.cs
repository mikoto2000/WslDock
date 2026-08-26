using WslDock.Application.Processes;
using WslDock.Infrastructure.Wsl;

namespace WslDock.Infrastructure.Tests;

public sealed class WslServiceTests
{
    [Fact]
    public async Task GetDistributions_uses_structured_wsl_arguments()
    {
        var runner = new FakeProcessRunner(new ProcessResult(0, "Ubuntu\r\n", ""));
        var service = new WslService(runner);

        var result = await service.GetDistributionsAsync();

        Assert.Equal("Ubuntu", Assert.Single(result).Name);
        Assert.Equal(["--list", "--quiet"], Assert.Single(runner.Requests).Arguments);
    }

    [Fact]
    public async Task GetDistributions_nonzero_throws_readable_error()
    {
        var service = new WslService(new FakeProcessRunner(new ProcessResult(1, "", "denied")));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDistributionsAsync());
        Assert.Contains("WSL", error.Message);
    }
}
