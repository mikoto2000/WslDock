namespace WslDock.Domain;

public static class DockerContainerStateExtensions
{
    public static bool CanStart(this DockerContainerState state) =>
        state is not (DockerContainerState.Running or DockerContainerState.Paused or DockerContainerState.Restarting);

    public static bool CanStop(this DockerContainerState state) =>
        state is DockerContainerState.Running or DockerContainerState.Paused;

    public static bool CanRemove(this DockerContainerState state) => CanStart(state);
}
