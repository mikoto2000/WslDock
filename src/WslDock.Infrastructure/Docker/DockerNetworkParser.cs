using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public static class DockerNetworkParser
{
    public static IReadOnlyList<DockerNetwork> Parse(string output) => DockerJsonLinesParser.Parse<Cli, DockerNetwork>(output, "network", x => new(x.ID ?? "", x.Name ?? "", x.Driver ?? "", x.Scope ?? ""));
    private sealed record Cli(string? ID, string? Name, string? Driver, string? Scope);
}
