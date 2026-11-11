namespace WslDock.Infrastructure.Process;
public sealed record TerminalLaunchRequest(string FileName, IReadOnlyList<string> Arguments);
