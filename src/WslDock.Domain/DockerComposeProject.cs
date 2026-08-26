namespace WslDock.Domain;
public sealed record DockerComposeProject(string Name, string Status, IReadOnlyList<string> ConfigFiles, string WorkingDirectory);
