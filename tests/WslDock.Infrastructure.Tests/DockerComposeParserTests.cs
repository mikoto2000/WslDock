using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class DockerComposeParserTests
{
    [Fact]
    public void Projects_parse_array_status_and_multiple_config_files()
    {
        const string json = """[{"Name":"one","Status":"running(2)","ConfigFiles":"/opt/app/compose.yml,/opt/app/override.yml"},{"Name":"two","Status":"exited(1)","ConfigFiles":"/home/me/app/compose.yml"}]""";
        var result = DockerComposeParser.ParseProjects(json);
        Assert.Equal(2, result.Count);
        Assert.Equal(["/opt/app/compose.yml", "/opt/app/override.yml"], result[0].ConfigFiles);
        Assert.Equal("/opt/app", result[0].WorkingDirectory);
    }

    [Fact] public void Projects_empty_array_is_empty() => Assert.Empty(DockerComposeParser.ParseProjects("[]"));
    [Fact] public void Projects_malformed_throws() => Assert.Throws<FormatException>(() => DockerComposeParser.ParseProjects("{"));

    [Fact]
    public void Services_parse_json_lines()
    {
        const string json = """
            {"Service":"web","Name":"demo-web-1","Image":"nginx","State":"running","Status":"Up","Ports":"80/tcp"}
            {"Service":"job","Name":"demo-job-1","Image":"busybox","State":"exited","Status":"Exited (0)","Ports":""}
            """;
        Assert.Equal(2, DockerComposeParser.ParseServices(json).Count);
    }
}
