# Docker Compose CLI notes

WslDock v1.1 was verified with Docker Compose v5.3.1 (the Compose v2 plugin invoked as `docker compose`).

- `docker compose ls --all --format json` returns a JSON array with `Name`, `Status`, and comma-separated `ConfigFiles`.
- `docker compose ps -a --format json` returns JSON Lines with `Service`, `Name`, `Image`, `State`, `Status`, and `Ports`.
- The observed config paths are absolute Linux/WSL paths. WslDock preserves them without Windows-path conversion.
- The project working directory is derived from the first config file's Linux directory.
- Every operation explicitly passes `-p <name>`, each config file as ordered `-f <path>`, and `--project-directory <directory>`.
- Paths remain separate `ProcessStartInfo.ArgumentList` values, so spaces do not require shell quoting.

Compose CLI exposes `ConfigFiles` as one comma-delimited JSON string. WslDock splits this field in CLI order. A future CLI/API that exposes a true array should replace this compatibility parser.
