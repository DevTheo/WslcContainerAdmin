# WSL containers (`wslc`): technical overview

**Research snapshot:** 2026-10-18. WSL and WSLc are evolving quickly; check the linked release notes and docs before relying on version-specific details.

## Terminology and status

- **WSL containers / WSLC** is Microsoft's Linux-container feature built into Windows Subsystem for Linux. `wslc.exe` is its command-line interface; Microsoft also ships a Windows-app API for embedding container operations. It is distinct from managing a user's ordinary WSL distributions.
- Microsoft announced general availability on **2026-09-29** with WSL release **3.0.1**. The release feed lists WSL **3.0.4** on 2026-10-17. Microsoft Learn documents a minimum of WSL **2.9.3 or later**; this is compatible with a later GA release, but does not itself establish which minimum is appropriate for every client. A future client should explicitly establish and test its supported WSL/wslc version rather than copy another app's requirement.
- `wslc` offers familiar container commands for images, builds, runs, containers, ports, logs, volumes, networks, and system operations. Familiar syntax does not establish full Docker CLI, daemon API, or Compose compatibility.

## Runtime architecture

Microsoft's architecture article describes this flow:

1. Client processes call the privileged Windows `wslservice.exe`, which can create VMs through HCS.
2. For a WSLc session, the service starts a separate `wslcsession.exe` child running on behalf of the calling user. The child performs session operations, reducing the privilege level for container work and isolating sessions in separate processes.
3. Each session has its own virtual disk for session state, including images, containers, networks, and volumes. The CLI's default storage is under the user's local app data; the base path can be changed in `wslc` settings.

This is a WSL-backed VM/session model, not a user-installed container daemon inside their normal Linux distro. The official overview says a separate engine install is not required for the built-in CLI.

## Storage and networking

- Windows-path volumes are shared into the Linux VM using **virtiofs**, then attached to containers as bind mounts. The architecture article describes virtiofs mounts under `/mnt` inside the VM.
- WSLc also supports VHD-backed named volumes. These provide a native Linux filesystem and an optional size limit; the CLI supports volume creation and attachment.
- The architecture article names the networking model **Consommé**. VM traffic is sent as Ethernet frames through a virtio queue to a Windows user-context networking process, which supplies DNS, TCP/UDP routing, and port mapping. Microsoft says this sends outbound traffic on behalf of the session's Windows user, aiming for VPN/firewall compatibility.
- The GA announcement also highlights `consomme` networking and improved Windows-file access performance. Treat comparative performance claims as Microsoft-reported, not independent measurements.

## CLI, API, and settings

- The CLI supports common lifecycle work (`build`, `run`, `exec`, list/inspect/logs, stop/restart, image operations), volumes and networks, published ports, and resource statistics. GA added, among other items, `container cp`, `system info`, network connect/disconnect, an event stream, health checks, and configurable default-session storage.
- Windows apps can use the `Microsoft.WSL.Containers` NuGet package. The API model is `WslcService` → `Session` → `Container` → `Process`; the docs describe image operations, container lifecycle, process stdin/stdout/stderr and signals, file mounts, networking, and GPU access. C#, C, and C++ projections are documented. The C++/WinRT projection is marked preview in the overview.
- The default CLI settings file is `%LOCALAPPDATA%\wslc\settings.yaml`. It can set session CPU count, memory, maximum storage, storage path, published-port binding address, host loopback name, idle timeout, and credential-store choice. Microsoft explicitly says `.wslconfig` does not configure WSLc sessions; API-created sessions use their own settings.
- Microsoft documents registry credentials via Windows Credential Manager by default, with a DPAPI-encrypted file option. The GA announcement describes Intune controls to disable WSL containers and allow-list registries, plus Microsoft Defender for Endpoint visibility for container activity.

## Compose and ecosystem status

At GA, Microsoft described native `wslc compose` support as its top feature request and stated it would be the focus of upcoming iterations, with the goal of using existing Compose YAML unchanged. Treat this as a roadmap statement, not a currently shipped native capability. Desktop clients can build orchestration above `wslc`; that is a separate implementation with its own compatibility contract.

The GA announcement also lists integrations for VS Code Dev Containers, the VS Code Containers extension, Aspire, a WSLc TUI, and community desktop clients. See [desktop-management-landscape.md](desktop-management-landscape.md).

## Practical research takeaways

- Identify the actual command/API version and capability baseline before designing UI behavior.
- Model session lifecycle and per-session storage separately from normal WSL distro lifecycle.
- Published ports, Windows-path mounts, credentials, networking and user-level security policy are core integration surfaces—not cosmetic details.
- Keep native WSLc capabilities distinct from features implemented by a desktop client or third-party orchestration layer.
- Validate operational behavior (session startup/shutdown, concurrent CLI calls, errors, mount semantics, policies) against current WSL releases; docs alone do not establish UX reliability.

## Sources

1. [WSL container overview and API](https://learn.microsoft.com/en-us/windows/wsl/wsl-container) — Microsoft Learn; updated 2026-10-16.
2. [Get started with containers on WSL](https://learn.microsoft.com/en-us/windows/wsl/tutorials/wsl-containers) — Microsoft Learn; CLI examples and prerequisites.
3. [WSL container settings](https://learn.microsoft.com/en-us/windows/wsl/wsl-container-settings) — Microsoft Learn; session settings and defaults.
4. [WSLC Architecture deep dive](https://devblogs.microsoft.com/commandline/wslc-architecture-deep-dive/) — Microsoft Windows Command Line blog; session, storage, and networking design; 2026-09-29.
5. [WSL containers is now generally available](https://blogs.windows.com/windowsdeveloper/2026/09/29/wsl-containers-now-generally-available/) — Microsoft Windows Developer Blog; GA scope, ecosystem, governance, and Compose roadmap; 2026-09-29.
6. [WSL releases](https://github.com/microsoft/WSL/releases) — Microsoft WSL GitHub releases; version/status evidence.
7. [WSL container API reference](https://wsl.dev/api-reference/) — language-projection reference.
