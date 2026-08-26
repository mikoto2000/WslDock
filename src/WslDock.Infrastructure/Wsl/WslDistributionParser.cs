using WslDock.Domain;

namespace WslDock.Infrastructure.Wsl;

public static class WslDistributionParser
{
    public static IReadOnlyList<WslDistribution> Parse(string output) => output
        .Replace("\0", string.Empty, StringComparison.Ordinal)
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => new WslDistribution(name))
        .ToArray();
}
