using WslDock.Domain;
using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class DockerContainerParserTests
{
    [Theory]
    [InlineData("running", DockerContainerState.Running)]
    [InlineData("exited", DockerContainerState.Exited)]
    [InlineData("future-state", DockerContainerState.Unknown)]
    public void Parse_maps_states(string state, DockerContainerState expected)
    {
        var json = $$"""{"ID":"abc","Names":"web","Image":"nginx","State":"{{state}}","Status":"Up","Ports":"80/tcp"}""";

        var container = Assert.Single(DockerContainerParser.Parse(json));
        Assert.Equal(expected, container.State);
        Assert.Equal("80/tcp", container.Ports);
    }

    [Fact]
    public void Parse_supports_multiple_lines_and_missing_ports()
    {
        const string output = """
            {"ID":"1","Names":"one","Image":"a","State":"created","Status":"Created","Ports":""}
            {"ID":"2","Names":"two","Image":"b","State":"paused","Status":"Paused"}
            """;

        var result = DockerContainerParser.Parse(output);
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(string.Empty, item.Ports));
    }

    [Fact]
    public void Parse_empty_output_returns_empty() => Assert.Empty(DockerContainerParser.Parse("  "));

    [Fact]
    public void Parse_invalid_json_throws_format_exception() =>
        Assert.Throws<FormatException>(() => DockerContainerParser.Parse("not json"));
}
