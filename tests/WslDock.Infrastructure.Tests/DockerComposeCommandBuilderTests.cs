using WslDock.Domain;
using WslDock.Infrastructure.Docker;

namespace WslDock.Infrastructure.Tests;

public sealed class DockerComposeCommandBuilderTests
{
    private static readonly DockerComposeProject Project = new("my project", "running(2)", ["/home/me/my app/compose.yml", "/home/me/my app/override.yml"], "/home/me/my app");

    [Theory]
    [InlineData("up", "-d")]
    [InlineData("down")]
    [InlineData("restart")]
    public void Build_includes_project_directory_and_ordered_files(params string[] operation)
    {
        var args = DockerComposeCommandBuilder.Build(Project, operation);
        Assert.Equal(["compose", "-p", "my project", "-f", "/home/me/my app/compose.yml", "-f", "/home/me/my app/override.yml", "--project-directory", "/home/me/my app"], args.Take(9));
        Assert.Equal(operation, args.Skip(9));
    }

    [Fact]
    public void Logs_include_tail_as_separate_argument() =>
        Assert.Equal(["logs", "--tail", "500"], DockerComposeCommandBuilder.Build(Project, ["logs", "--tail", "500"]).TakeLast(3));
}
