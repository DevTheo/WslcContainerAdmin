# Wslc Container Admin

A Windows desktop client for managing WSL containers through the installed `wslc.exe` command-line interface. The application does not use the WSLc SDK and does not start a background service.

## Requirements

- Windows 10 version 2004 (build 19041) or newer.
- WSLc installed, with `wslc.exe` available on `PATH` for the same Windows user who launches the app.
- .NET 10 SDK to build and run from source. The Release publish is self-contained and does not require a separately installed .NET runtime.

The application does not bundle `wslc.exe`; WSLc installation, registry credentials, and policy remain managed by WSLc.

## Build and run

```powershell
cd src/WslcContainerAdmin
dotnet run
```

## Publish

```powershell
cd src/WslcContainerAdmin
dotnet publish -c Release -r win-x64
```

The Release configuration enables self-contained, single-file publishing for `win-x64`. The executable is written under `bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/`.

## Current features

- **Containers:** list running and stopped containers; start, stop, inspect, follow logs, poll stats every two seconds, run detached containers, and remove containers after confirmation.
- **Run wizard:** choose a local image or enter an image reference, then configure a container name, command, command arguments, port mappings, volume mounts, and environment variables. Map each host port to a container port; when a listed image is selected, its exposed ports are offered as one-click container-port defaults. Select an existing named volume and enter its container path for each volume mount. The command field takes one executable; enter its arguments one per line. Environment-variable lines are passed separately to `wslc`. A missing image may be pulled by WSLc's default `run` behavior.
- **Images:** list local images, show whether WSLc reports a container reference, pull an image reference, start the run wizard from a selected image, and remove an image after confirmation.
- **Volumes:** create named volumes, show whether any running or stopped container references them, and remove selected volumes after confirmation. The usage indicator is derived by inspecting every listed container's mounts; inspect failures surface as errors rather than marking a volume unused.
- **Networks:** read-only list with refresh.

The app invokes WSLc with argument-safe process invocation. It parses the CLI's JSON-lines output for container, image, volume, and network lists. Container inspect output is shown as JSON; logs are followed from the CLI process's standard output.

## Session behavior

The app uses the default WSLc session. It does not expose a session selector and does not pass the global `--session` option. Every operation runs as the Windows user who launched the app.
