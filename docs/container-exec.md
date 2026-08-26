# Container Exec

WslDock launches `wsl.exe` directly from its GUI process with `UseShellExecute = true`, no redirected streams, and a visible console. Windows allocates/delegates that console to the user's configured default terminal. This avoids a hard dependency on `wt.exe` and avoids `cmd.exe` command-string quoting.

The terminal request uses structured arguments:

```text
wsl.exe
  -d <selected distribution>
  -- docker exec -it <container ID> <detected shell>
```

Before launch, `WslContainerExecService` executes non-interactive probes through `IProcessRunner` in this order:

```text
docker exec <container ID> test -x /bin/bash
docker exec <container ID> test -x /bin/sh
```

The second probe runs only when bash is absent. Docker/WSL errors are reported to the WPF error area; two clean non-zero probe results mean the container has no supported shell.

The interactive process is launched through `ITerminalLauncher`. WslDock waits only for process creation, not for the terminal session to exit, so the application immediately leaves its Busy state.

On the Windows 11 smoke-test host, console delegation opened Windows Terminal and produced a persistent `WslDock.App.exe -> wsl.exe` interactive process tree. The implementation does not call `wt.exe`, so another configured default terminal can be selected by Windows.
