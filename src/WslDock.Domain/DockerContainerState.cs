namespace WslDock.Domain;

public enum DockerContainerState
{
    Unknown,
    Running,
    Exited,
    Paused,
    Restarting,
    Created,
    Dead
}
