using System.Text.Json;
using WslDock.Domain;

namespace WslDock.Infrastructure.Docker;

public static class DockerContainerParser
{
    public static IReadOnlyList<DockerContainer> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return [];

        var result = new List<DockerContainer>();
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var item = JsonSerializer.Deserialize<CliContainer>(line)
                    ?? throw new JsonException("Container data was null.");
                result.Add(new DockerContainer(
                    item.ID ?? string.Empty,
                    item.Names ?? string.Empty,
                    item.Image ?? string.Empty,
                    ParseState(item.State),
                    item.Status ?? string.Empty,
                    item.Ports ?? string.Empty));
            }
            catch (JsonException exception)
            {
                throw new FormatException("Docker returned invalid container JSON.", exception);
            }
        }

        return result;
    }

    private static DockerContainerState ParseState(string? state) =>
        Enum.TryParse<DockerContainerState>(state, true, out var parsed) ? parsed : DockerContainerState.Unknown;

    private sealed record CliContainer(string? ID, string? Names, string? Image, string? State, string? Status, string? Ports);
}
