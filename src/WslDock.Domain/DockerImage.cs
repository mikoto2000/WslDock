namespace WslDock.Domain;
public sealed record DockerImage(string Id, string Repository, string Tag, string Digest, string Created, string Size);
