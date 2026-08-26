namespace WslDock.Domain;
public sealed record DockerNetwork(string Id, string Name, string Driver, string Scope)
{
    public bool IsStandard => Name is "bridge" or "host" or "none";
}
