using System.Text.Json;

namespace WslDock.Infrastructure.Docker;
internal static class DockerJsonLinesParser
{
    public static IReadOnlyList<TOutput> Parse<TInput, TOutput>(string output, string resource, Func<TInput, TOutput> map)
    {
        if (string.IsNullOrWhiteSpace(output)) return [];
        try
        {
            return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => JsonSerializer.Deserialize<TInput>(line) ?? throw new JsonException("Null JSON item."))
                .Select(map).ToArray();
        }
        catch (JsonException exception) { throw new FormatException($"Docker returned invalid {resource} JSON.", exception); }
    }
}
