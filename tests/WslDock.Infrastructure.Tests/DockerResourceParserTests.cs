using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class DockerResourceParserTests
{
    [Fact]
    public void Images_parse_multiple_and_none_values()
    {
        const string json = """
            {"ID":"sha256:1","Repository":"nginx","Tag":"latest","Digest":"sha256:x","CreatedSince":"2 weeks ago","Size":"190MB"}
            {"ID":"sha256:2","Repository":"<none>","Tag":"<none>","Digest":"<none>","CreatedSince":"1 day ago","Size":"10MB"}
            """;
        var result = DockerImageParser.Parse(json);
        Assert.Equal(2, result.Count);
        Assert.Equal("<none>", result[1].Repository);
        Assert.Equal("2 weeks ago", result[0].Created);
    }

    [Fact] public void Images_empty_is_empty() => Assert.Empty(DockerImageParser.Parse(""));
    [Fact] public void Images_malformed_throws() => Assert.Throws<FormatException>(() => DockerImageParser.Parse("{"));

    [Fact]
    public void Volumes_parse_multiple()
    {
        const string json = """
            {"Name":"one","Driver":"local","Scope":"local"}
            {"Name":"two","Driver":"local","Scope":"global"}
            """;
        Assert.Equal(2, DockerVolumeParser.Parse(json).Count);
    }

    [Fact] public void Volumes_empty_is_empty() => Assert.Empty(DockerVolumeParser.Parse(""));
    [Fact] public void Volumes_malformed_throws() => Assert.Throws<FormatException>(() => DockerVolumeParser.Parse("oops"));

    [Theory]
    [InlineData("bridge", "bridge", true)]
    [InlineData("host", "host", true)]
    [InlineData("none", "null", true)]
    [InlineData("my-net", "bridge", false)]
    [InlineData("custom", "overlay-x", false)]
    public void Networks_mark_standard_names(string name, string driver, bool standard)
    {
        var json = $$"""{"ID":"id","Name":"{{name}}","Driver":"{{driver}}","Scope":"local"}""";
        Assert.Equal(standard, Assert.Single(DockerNetworkParser.Parse(json)).IsStandard);
    }

    [Fact] public void Networks_empty_is_empty() => Assert.Empty(DockerNetworkParser.Parse(""));
    [Fact] public void Networks_malformed_throws() => Assert.Throws<FormatException>(() => DockerNetworkParser.Parse("["));
}
