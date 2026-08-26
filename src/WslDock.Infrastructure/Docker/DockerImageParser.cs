using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public static class DockerImageParser
{
    public static IReadOnlyList<DockerImage> Parse(string output) => DockerJsonLinesParser.Parse<Cli, DockerImage>(output, "image", x => new(x.ID ?? "", x.Repository ?? "", x.Tag ?? "", x.Digest ?? "", x.CreatedSince ?? "", x.Size ?? ""));
    private sealed record Cli(string? ID, string? Repository, string? Tag, string? Digest, string? CreatedSince, string? Size);
}
