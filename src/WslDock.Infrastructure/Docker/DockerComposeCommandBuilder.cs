using WslDock.Domain;
namespace WslDock.Infrastructure.Docker;
public static class DockerComposeCommandBuilder
{
    public static IReadOnlyList<string> Build(DockerComposeProject project, IReadOnlyList<string> operation)
    {
        var args = new List<string> { "compose", "-p", project.Name };
        foreach (var file in project.ConfigFiles) { args.Add("-f"); args.Add(file); }
        args.Add("--project-directory"); args.Add(project.WorkingDirectory);
        args.AddRange(operation);
        return args;
    }
}
