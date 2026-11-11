namespace WslDock.Domain;

public sealed record DockerComposeService(
    string Id,
    string Service,
    string ContainerName,
    string Image,
    DockerContainerState State,
    string Status,
    string Ports);
