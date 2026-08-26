namespace WslDock.Domain;
public sealed record DockerComposeService(string Service, string ContainerName, string Image, string State, string Status, string Ports);
