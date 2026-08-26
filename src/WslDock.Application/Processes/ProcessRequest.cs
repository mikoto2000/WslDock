namespace WslDock.Application.Processes;

public sealed record ProcessRequest(string Executable, IReadOnlyList<string> Arguments)
{
    public ProcessRequest(string executable, params string[] arguments) : this(executable, (IReadOnlyList<string>)arguments) { }
}
