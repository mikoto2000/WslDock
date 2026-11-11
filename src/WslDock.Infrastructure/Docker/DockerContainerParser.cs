using WslDock.Domain;

namespace WslDock.Infrastructure.Docker;

public static class DockerContainerParser
{
    public static IReadOnlyList<DockerContainer> Parse(string output) =>
        DockerJsonLinesParser.Parse<CliContainer, DockerContainer>(output, "container", item => new(
            item.ID ?? string.Empty,
            item.Names ?? string.Empty,
            item.Image ?? string.Empty,
            ParseState(item.State),
            item.Status ?? string.Empty,
            item.Ports ?? string.Empty));

    internal static DockerContainerState ParseState(string? state) =>
        Enum.TryParse<DockerContainerState>(state, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : DockerContainerState.Unknown;

    private sealed record CliContainer(string? ID, string? Names, string? Image, string? State, string? Status, string? Ports);
}
