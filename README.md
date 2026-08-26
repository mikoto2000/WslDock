# WslDock

WslDock is a lightweight Windows desktop application for managing Docker Engine running inside WSL2.

## Overview

WslDock provides a native WPF interface for everyday management of Docker containers in a selected WSL2 distribution. Version 1 invokes the Docker CLI through `wsl.exe`; it does not require Docker Desktop and does not connect directly to the Docker Engine API.

## Requirements

- Windows 11 with WSL2 enabled
- At least one installed WSL2 distribution
- Docker CLI and Docker Engine installed in the target distribution
- .NET 10 Desktop Runtime, or a self-contained publish

## Build and test

```powershell
dotnet restore
dotnet build
dotnet test
dotnet build -c Release
```

## Run

```powershell
dotnet run --project src/WslDock.App/WslDock.App.csproj
```

Choose a WSL distribution, confirm the engine status, select a container, and use Start, Stop, Restart, or Logs. Successful state-changing operations refresh the list automatically.

Configuration is stored in `src/WslDock.App/appsettings.json`:

- `DefaultDistribution`: preferred WSL distribution name (or `null`)
- `LogTailLines`: maximum historical log lines, default `500`

## v1 features

- WSL distribution discovery and selection
- Docker Engine availability classification
- Container listing with name, image, state, status, and ports
- Start, stop, restart, and historical logs
- Manual refresh, busy-state command protection, cancellation on exit, and concise errors
- Structured process arguments and debug logging

## Non-goals

Version 1 does not manage images, volumes, networks, Compose projects, container creation/deletion, exec sessions, streaming events/stats, Kubernetes, registry login, daemon installation, or daemon configuration.

## Architecture

```text
WslDock.App            -> WslDock.Application -> WslDock.Domain
WslDock.Infrastructure -> WslDock.Application -> WslDock.Domain
```

`WslDock.App` contains WPF/MVVM and dependency composition. `WslDock.Application` defines use-case boundaries. `WslDock.Domain` contains pure models. `WslDock.Infrastructure` owns process execution, WSL parsing, and Docker CLI parsing/calls. Unit tests use fake process and Docker clients and do not require WSL or Docker.
