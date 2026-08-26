using WslDock.Application.Processes;

namespace WslDock.Infrastructure.Tests;

public sealed class ProcessResultTests
{
    [Fact] public void Exit_zero_is_success() => Assert.True(new ProcessResult(0, "ok", "").IsSuccess);
    [Fact] public void Non_zero_with_stderr_is_failure()
    {
        var result = new ProcessResult(1, "", "failed");
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.StandardError);
    }
}
