using System.Text.Json;
using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public static class DockerComposeParser
{
    public static IReadOnlyList<DockerComposeProject> ParseProjects(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return [];
        try
        {
            return (JsonSerializer.Deserialize<CliProject[]>(output) ?? []).Select(x =>
            {
                var files = (x.ConfigFiles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var directory = files.Length == 0 ? "." : LinuxDirectory(files[0]);
                return new DockerComposeProject(x.Name ?? "", x.Status ?? "", files, directory);
            }).ToArray();
        }
        catch (JsonException exception) { throw new FormatException("Docker Compose returned invalid project JSON.", exception); }
    }

    public static IReadOnlyList<DockerComposeService> ParseServices(string output) =>
        DockerJsonLinesParser.Parse<CliService, DockerComposeService>(output, "Compose service", x => new(x.ID ?? "", x.Service ?? "", x.Name ?? x.Names ?? "", x.Image ?? "", DockerContainerParser.ParseState(x.State), x.Status ?? "", x.Ports ?? ""));

    private static string LinuxDirectory(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash <= 0 ? (slash == 0 ? "/" : ".") : path[..slash];
    }
    private sealed record CliProject(string? Name, string? Status, string? ConfigFiles);
    private sealed record CliService(string? ID, string? Service, string? Name, string? Names, string? Image, string? State, string? Status, string? Ports);
}
