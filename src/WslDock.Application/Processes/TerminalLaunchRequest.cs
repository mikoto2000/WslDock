namespace WslDock.Application.Processes;
public sealed record TerminalLaunchRequest(string FileName, IReadOnlyList<string> Arguments);
