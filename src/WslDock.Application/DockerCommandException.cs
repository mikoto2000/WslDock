namespace WslDock.Application;

public sealed class DockerCommandException(string message) : Exception(message);
