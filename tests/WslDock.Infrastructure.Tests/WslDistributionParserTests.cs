using WslDock.Infrastructure.Wsl;

namespace WslDock.Infrastructure.Tests;

public sealed class WslDistributionParserTests
{
    [Fact]
    public void Parse_returns_names_and_removes_null_characters()
    {
        var result = WslDistributionParser.Parse("Ubuntu\0\r\nDebian\0\r\n");

        Assert.Equal(["Ubuntu", "Debian"], result.Select(x => x.Name));
    }

    [Fact]
    public void Parse_ignores_empty_lines() =>
        Assert.Empty(WslDistributionParser.Parse(" \r\n\0\r\n"));
}
