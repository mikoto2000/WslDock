namespace WslDock.Domain;

public enum DockerEngineStatus
{
    Unavailable,
    Running,
    CliNotInstalled,
    DaemonUnavailable
}
