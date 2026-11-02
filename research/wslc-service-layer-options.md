# WSLc service-layer options

**Research snapshot:** 2026-10-18. On 2026-10-26, the user chose `wslc.exe` as the sole WSLc integration surface for this desktop app; the broader web and remote architecture questions below remain open.

## User-stated context

- The user often installs Portainer CE on machines running Docker or Podman.
- They value Portainer's screens for editing live containers and its presentation of complicated deployments in a way that does not require knowing the full command line.
- They are undecided about developing a full web app. WASM is one possibility, but they recognize it would require a service layer and raise security work they may not want to take on now.
- These are UX preferences and architecture concerns, not a decision to build a Portainer clone, a web app, or a remote management product.

## Current implementation decision (2026-10-26)

- All WSLc operations in this desktop app use the installed `wslc.exe` CLI. No WSLc SDK is used.
- The app uses the default WSLc session; it has no session selector and does not pass the global `--session` option.
- This decision is scoped to the current native desktop app. It does not select a web/WASM hosting model or a remote management plane.

- On 2026-10-29, the user confirmed Avalonia as the UI framework and asked to use this app's existing WSLc service layer. The current `WslcContainerService` is an in-process app service backed by `IWslcCli`; this does not introduce a separate host service or WinUI dependency.

## “Service layer” can mean different things

1. **In-process application services:** UI/view models call app-owned services that encapsulate WSLc operations. This is a code-organization boundary inside a desktop application; it is not inherently a separately running service or network endpoint.
2. **WSLc platform API:** Microsoft documents a Windows-app API (`Microsoft.WSL.Containers`) with C#, C, and C++ projections. Its documented object model is `WslcService` → `Session` → `Container` → `Process`. `WslcService` is the API's static entry point for service-level operations; the docs describe an SDK/API, not a persistent HTTP daemon.
3. **CLI integration:** A client can invoke `wslc.exe` for operations exposed by the CLI. This is another WSLc integration surface, not by itself a web service.
4. **Local host bridge/backend:** A browser-hosted UI, including a WASM UI, cannot itself invoke Windows executables or the WSLc Windows API directly. It would need a native host integration, such as a companion backend or a desktop shell with a constrained IPC bridge. This is an architectural inference; the exact WASM hosting model has not been selected or researched in depth.
5. **Remote management plane:** Portainer documents a server/API model that can manage container environments, with agents for connected environments. This is a wider networked control plane, not a requirement implied by using Portainer's UI as a reference.

## What the sources establish

- The WSL container API is available to Windows applications as a NuGet package; Microsoft documents operations for sessions, containers, processes, image management, streams, mounts, networking, and GPU access. The WSLc architecture article says the privileged WSL service starts a session process on behalf of the calling user, which performs session work in a less-privileged process.
- The official WSLc docs reviewed here describe native API projections and a CLI. They do not document a WSLc HTTP/REST service endpoint.
- Portainer documents a Portainer Server and Agent architecture, an HTTP API that powers the UI and can proxy requests to underlying Docker/Kubernetes APIs, and API access tokens. Its security documentation describes role/policy checks and the risk of how the management plane is configured and exposed.
- Portainer's documented architecture is a reference for a full management plane. It does not establish that a local WSLc desktop manager needs a server or agent.

## Decision frame, not a recommendation

- A native Windows desktop app could have an internal service layer that calls the WSLc API or CLI without introducing a separate localhost HTTP service. This is a plausible option, not a chosen architecture.
- A web/WASM frontend needs some host-side capability boundary to perform WSLc operations. A WebView/native shell may use narrow IPC; a browser-based app may use a local backend. Either adds a security boundary and contract to design.
- A remote server/agent model would support broader remote/multi-host management, but adds deployment, identity, authorization, transport, state, and operational responsibilities. Whether that scope is useful is undecided.

## Security questions to carry forward

Container management is powerful: operations can start processes, mount host paths, change networking, and interact with registry credentials. If a web UI is considered, research and decide explicitly:

- Which OS user identity executes requests, and whether any component runs with elevated privileges.
- Whether the host interface is IPC-only or network-accessible; if network-accessible, how authentication, authorization, origin/CSRF protection, binding, and transport are enforced.
- Which actions are permitted to the UI, how destructive operations are authorized, and how organizational WSLc policies remain enforced.
- How registry credentials, request arguments, process output, logs, and audit records are handled.
- How the UI/backend protocol is versioned and how concurrent or long-running WSLc operations are represented and cancelled.

These are unresolved design questions, not declared requirements. A local native app still needs safe command/API handling, but a separate web service is not assumed.

## Sources

1. [Microsoft Learn: WSL container overview and API](https://learn.microsoft.com/en-us/windows/wsl/wsl-container) — WSLc SDK, object model, and Windows application integration.
2. [WSL container API reference](https://wsl.dev/api-reference/) — C, C#, and C++ projections.
3. [Microsoft: WSLC Architecture deep dive](https://devblogs.microsoft.com/commandline/wslc-architecture-deep-dive/) — service/session process model.
4. [Portainer architecture](https://docs.portainer.io/start/architecture) — Server, Agent, and connectivity patterns.
5. [Portainer API documentation](https://docs.portainer.io/api/docs) — HTTP API, gateway role, and access tokens.
6. [Portainer security and compliance](https://docs.portainer.io/portainer-documentation/2.39-lts/advanced/security) — management-plane access responsibilities and hardening concerns.
