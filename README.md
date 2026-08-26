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

## Release package

Push a tag beginning with `v` to build a self-contained Windows x64 ZIP and attach it to a GitHub Release:

```powershell
git tag v1.2.0
git push origin v1.2.0
```

The **Build and release** workflow can also be run manually. Manual runs upload the ZIP as a workflow artifact without creating a GitHub Release.

Choose a WSL distribution, confirm the engine status, select a container, and use Start, Stop, Restart, or Logs. Successful state-changing operations refresh the list automatically.

Configuration is stored in `src/WslDock.App/appsettings.json`:

- `DefaultDistribution`: preferred WSL distribution name (or `null`)
- `LogTailLines`: maximum historical log lines, default `500`

## v1.1 features

- WSL distribution discovery and selection
- Docker Engine availability classification
- Container listing with name, image, state, status, and ports
- Start, stop, restart, confirmed safe removal, and historical logs
- Interactive Exec for running containers in a new Windows default terminal
- Image listing, pull, safe remove, and confirmed unused-image prune
- Volume and network listing, safe remove, and confirmed prune
- Protection for Docker's `bridge`, `host`, and `none` networks
- Docker Compose project discovery, service state, up, restart, logs, and confirmed down
- Manual refresh, busy-state command protection, cancellation on exit, and concise errors
- Structured process arguments and debug logging

Docker Compose management requires the Docker Compose v2 plugin (`docker compose`). Compose v1 (`docker-compose`) is not supported.

## Non-goals

Version 1.1 does not provide builds, image push/tag, registry login, resource creation UI, Compose editing/new-project creation, exec sessions, streaming events/stats, Kubernetes, daemon installation, or daemon configuration.

## Architecture

```text
WslDock.App            -> WslDock.Application -> WslDock.Domain
WslDock.Infrastructure -> WslDock.Application -> WslDock.Domain
```

`WslDock.App` contains WPF/MVVM and dependency composition. `WslDock.Application` defines use-case boundaries. `WslDock.Domain` contains pure models. `WslDock.Infrastructure` owns process execution, WSL parsing, and Docker CLI parsing/calls. Unit tests use fake process and Docker clients and do not require WSL or Docker.

```text
WPF View -> ViewModel -> Application abstraction -> Infrastructure CLI client
         -> IProcessRunner -> wsl.exe -> docker / docker compose -> Docker Engine
```

All resource screens share one selected WSL distribution context. Long-running operations remain asynchronous, and CLI values are passed as individual process arguments rather than shell command strings.

### Container Exec

Select a running container and choose **Exec** to open an interactive shell in a new Windows terminal. WslDock checks `/bin/bash` first, falls back to `/bin/sh`, and reports an error without opening a terminal when neither shell exists. The selected WSL distribution and its Docker CLI are used. WslDock does not embed a terminal or redirect the interactive session through WPF.

```text
Containers ViewModel -> Container Exec service -> wsl.exe / docker exec test
                     -> Terminal launcher -> Windows default terminal
                     -> wsl.exe -> docker exec -it -> Container shell
```
