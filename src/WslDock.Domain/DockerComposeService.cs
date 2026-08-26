namespace WslDock.Domain;
public sealed record DockerComposeService(string Id, string Service, string ContainerName, string Image, string State, string Status, string Ports);
