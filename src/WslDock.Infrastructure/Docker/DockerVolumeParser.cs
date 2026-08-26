using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public static class DockerVolumeParser
{
    public static IReadOnlyList<DockerVolume> Parse(string output) => DockerJsonLinesParser.Parse<Cli, DockerVolume>(output, "volume", x => new(x.Name ?? "", x.Driver ?? "", x.Scope ?? ""));
    private sealed record Cli(string? Name, string? Driver, string? Scope);
}
