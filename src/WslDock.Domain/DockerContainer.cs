namespace WslDock.Domain;

public sealed record DockerContainer(
    string Id,
    string Name,
    string Image,
    DockerContainerState State,
    string Status,
    string Ports);
