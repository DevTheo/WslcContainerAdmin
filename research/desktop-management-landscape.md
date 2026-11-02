# Desktop tools for WSL containers

**Research snapshot:** 2026-10-18. Repository status, features, and compatibility can change; links below are project/official sources, not independent quality reviews.

## Finding

There are already graphical tools for managing WSL containers. Microsoft's 2026-09-29 GA announcement explicitly names **WSL Container Desktop** as a community desktop app for WSL containers, Kubernetes (k3s), and container registries. It also names a separate Electron app, **WSLC Desktop**. The initial research premise that no desktop tool exists is therefore not accurate; any future product opportunity needs to be differentiated by the user's opinions, requirements, and validated gaps—not by absence of existing apps.

## Identified tools and integrations

| Project | Form and stated scope | Evidence and caveats |
| --- | --- | --- |
| [WSL Container Desktop](https://github.com/mhackermsft/wslcontainerdesktop) | Community WinUI 3 / .NET 10 desktop app. Its README describes WSLc container/image/volume/network management, Compose orchestration, k3s, registries, tray presence, and optional AI features. | The Microsoft GA post lists it as a community desktop contribution. The project README says it is independent, not a Microsoft product, and targets WSL 3.0.1+. The README describes Compose as orchestration above `wslc`; Microsoft still described native `wslc compose` as future work at GA. Project capabilities and compatibility are project claims, not independently tested here. |
| [WSLC Desktop](https://github.com/bacarndiaye/wslc-desktop) | Separate Electron desktop client. Its README describes container, image, volume, and network views/actions, logs, inspect, and a session indicator; it says the UI shells out to `wslc.exe`. | The README calls it a preview-era app and documents preview limitations. That text may be stale after WSLc GA, so verify its latest releases and actual behavior before treating those limits as current. The repo metadata identifies an MIT license. |
| [Lazywslc](https://github.com/craigloewen-msft/lazywslc) | Terminal UI for WSL containers. | Named by Microsoft's GA announcement; not a graphical desktop app. |
| [VS Code Containers extension](https://github.com/microsoft/vscode-containers/releases/tag/microsoft-vscode-container-client-v1.1.0) and [Dev Containers CLI](https://github.com/devcontainers/cli/blob/main/CHANGELOG.md#0880) | Editor/developer-tool integrations. Microsoft's GA announcement says each supports `wslc` in its stated role. | Useful context for workflows; not a general Windows desktop management UI. |
| [Aspire](https://aspire.dev/) | Application orchestration/development platform. | Microsoft's GA announcement says Aspire can use WSL Containers as a first-class container runtime; not a general-purpose desktop manager. |

## What this establishes—and what it does not

- At least one directly relevant desktop manager exists, and a second separate desktop client is publicly listed. The specific WSL Container Desktop project is not a Microsoft product despite being mentioned by Microsoft.
- Existence is not evidence that either app matches this user's needs, is actively maintained, is reliable, or has the desired UX. This research did not build/run the applications, inspect their complete code, compare release cadence, or conduct a feature-by-feature hands-on review.
- The two clients make different product choices and claim different breadth. WSL Container Desktop advertises a broad control plane including k3s and Compose; WSLC Desktop describes a smaller CLI-backed manager. Neither should be presumed to be the intended baseline until the user shares their opinions.
- WSLc itself and its client ecosystem are new. Microsoft's GA announcement says native Compose is a future focus; a client that offers Compose today necessarily has a separate implementation layer whose supported YAML semantics should be evaluated on its own.

## User-stated product context (2026-10-18)

- The user often installs Portainer CE on machines running Docker or Podman. They value its screens for editing live containers and how it presents complicated deployments in ways that do not require knowing the full command line.
- The user is undecided about building a full web app. WASM is one possibility they are considering; they recognize that it would need a host-side service/bridge and that service-layer security adds scope they may not want to take on now.
- Treat Portainer as a user-provided UX reference, not a decision to clone its UI, adopt its server/agent architecture, or support Docker and Podman. The user has chosen a native desktop app using `wslc.exe` for this project; broader product scope remains open.

## Screen-reference direction (2026-10-29)

- The user wants to use [WSL Container Desktop](https://github.com/mhackermsft/wslcontainerdesktop) as a screen/UX reference while rewriting this app in Avalonia and using the existing in-process WSLc service layer. This is a UI reference, not a request to adopt WinUI 3.
- The user does not consider notifications important; do not treat the reference app's toast-notification screen/features as part of the desired design absent a later request.
- The reviewed [dashboard screenshot](https://github.com/mhackermsft/wslcontainerdesktop/blob/main/docs/screenshots/dashboard.png) uses summary cards, engine status, aggregate CPU, and a live per-container performance table. The [containers screenshot](https://github.com/mhackermsft/wslcontainerdesktop/blob/main/docs/screenshots/containers.png) uses persistent navigation, grouped container rows, status/port context, and inline actions. These are reference patterns; the app's broader advertised capabilities (such as Kubernetes, Compose, AI, templates, and registries) are not implied requirements.
- The project identifies itself as GPL-3.0. Treat its screens as visual/interaction references; review license compatibility before copying source code or assets.

The user selected the existing WSLc screen set (containers, container details, run-container wizard, images, volumes, and networks), not the reference app's broader feature portfolio. They specifically want viewmodels and no interaction/business logic in code-behind. The current Avalonia `.axaml.cs` files only call `InitializeComponent`.

- When adapting screenshots, the user prioritizes the data and meaningful controls over pixel-exact layout; preserve familiar layout patterns where they fit the app. Do not add a field solely to match a reference if WSLc does not expose it or the value is effectively invariant/default-only.

## Container-list ideas from the user (2026-10-26)

The user likes Portainer's feature set and may borrow some capabilities, but not everything. These are requested candidates, not a finalized implementation list:

- Search containers; provide start, stop, kill, restart, pause/resume, remove, and add-container actions.
- Make each container name link to its details page.
- Provide icon quick links for Logs, Inspect, Stats, Exec Console, and Attach Console.
- Make published ports clickable and open a browser window. The user is interested in inferring HTTP versus HTTPS while recognizing that a published port may not serve web traffic.
- The user expects a Back action on container details that returns to the originating containers list; apply the same pattern to any future detail screens.
- The user wants a terminal action on container details that opens a separate Windows Terminal window connected to the running container. Use WSLc `container exec` with interactive and TTY enabled; the default shell is `/bin/sh`.

## Container-creation setup ideas from the user (2026-10-26)

The user considers the current container setup poor and described these UX changes as desired direction; implementation details are not yet specified:

- For volumes, select a volume that has already been created and specify the container path, rather than entering a volume mapping as free-form text.
- For ports, provide a similar selection/mapping experience, let the user select any port, and determine which ports are exported by the container when possible.

The precise meaning of “any port” (host-side, container-side, or both), how exported ports are discovered, and whether this applies to image metadata or an existing container remain open. Treat those as design questions, not settled behavior.

On 2026-10-29, the user described a Portainer-inspired create-container layout: image selection/pull first, port mappings next, then Advanced container settings in tabs (Commands & Logging, Volumes, Network, Env, Labels, Restart Policy, Runtime & resources, Capabilities). The tab layout is a UX reference; only controls supported by the installed WSLc CLI should be wired up.

The installed `wslc container run --help` documents support for entrypoint, working directory, user, interactive/TTY modes, network, hostname, domain name, IPv4, DNS, env/env-file, labels, memory, CPU limit, shared-memory size, GPU pass-through, and stop timeout. It does not document logging-driver, restart-policy, MAC/IPv6/host-file, privileged/init/runtime/device/sysctl, memory-reservation, or capability flags. Those unsupported controls should not be presented as functional options.

The user selected the full WSLc-supported subset for the Avalonia setup UI, rather than a narrower staged subset; unsupported Portainer controls remain excluded.

On 2026-10-29, the user requested run-container controls for background/detached mode, remove-on-exit, keep STDIN open, pass all GPUs, and stop timeout. Local `wslc container run --help` confirms `--detach`, `--rm`, `--interactive`, `--gpus all`, and `--stop-timeout` (seconds; `-1` means no timeout). The existing detached default is preserved; other options default off, and an empty timeout uses the WSLc default.

The user reports that `wslc image ls` lists installed images and returns image IDs usable with `wslc image inspect <image-id>`. Their MSSQL image inspection (`f634a44c5d75`) did not show exported ports, while inspection of a pulled nginx image did. Image inspection can therefore provide some image-defined ports, but discovery is best-effort and image-dependent.

### WSLc CLI feasibility checked locally

- `wslc container` help lists start, stop, kill, restart, remove, run/create, logs, inspect, stats, exec, and attach. `pause`, `unpause`, and `port` are not CLI commands in the installed version.
- Search and name-to-details navigation are desktop UI behavior, not CLI operations.
- The CLI provides container-list port text and inspect data, but the current machine had no containers to validate published-port parsing or host URLs. Scheme inference and behavior for non-HTTP services remain unresolved; do not imply that every port opens in a browser.
- Exec and attach are distinct terminal workflows; whether the Avalonia UI can provide the required interactive stdin/TTY behavior needs implementation validation.


## Image and volume browsing observations (2026-10-30)

- The user noted that Portainer shows a “Date pulled” for images and a “Created date” for volumes. The volume details view includes created date, mount path, driver, and containers using the volume; the user considers Access Control unnecessary and suspects the driver may be redundant.
- The user wants the volume’s containers listed with links to their container details.
- Local `wslc image list --format json` includes `CreatedAt` and relative `CreatedSince`; the Images list continues to show `CreatedSince`. The image detail screen now displays inspect-backed `Created`, platform, repo tags, configuration, and RootFS layer digests.
- Local `wslc volume list --format json` did not include a creation date. `wslc volume inspect portainer_data` did return `CreatedAt`, `Mountpoint`, `Driver`, and `Scope`. The observed volume had driver `guest` and scope `local`, so the driver is not literally always `local` on this host.
- Volume-to-container relationships are discovered by `WslcResourceDetailsService` through `container inspect` `Mounts[].Name`. The volume-details screen lists matching containers with links to container details; its Delete button is enabled only when the service confirms the volume is unused. The separate list-level remove command remains not usage-gated.
- The user wants the volume-details delete action enabled only when no containers use that volume. `IsUnused == null` remains ineligible; the new details screen uses a positive unused result before enabling deletion.


### Image details screen reference (2026-10-30)

- The user supplied Portainer screenshots showing image tags and tag/pull/push/delete actions; image metadata (ID, size, creation date, platform/build); Dockerfile configuration (CMD, ENTRYPOINT, EXPOSE, ENV); and ordered layer rows with sizes and build instructions.
- Local `wslc image inspect` output exposes ID, Size, Created, RepoTags, OS/Architecture, Config values (Cmd, Entrypoint, ExposedPorts, Env, Labels), and RootFS layer digests. The image details screen now displays these inspect-backed fields.
- RootFS layer digests are available, but the inspected WSLc output and local CLI have no image-history data or per-layer instruction/size records. Do not fabricate the Portainer layer history rows.
- `Created` is not established as local pull time; label it as creation date, not “Date pulled.” The local CLI also supports image tag/push, but the current detail screen does not expose those actions.
## Network-list screen reference (2026-10-30)

- The user supplied a Portainer network-list screenshot with Name, Stack, Driver, Attachable, IPAM Driver, IPv4/IPv6 subnet and gateway, and Ownership fields. The user prioritizes useful data over reproducing the exact layout; fields that are unavailable or effectively invariant need not be added.
- The current Avalonia Networks view shows Name, Driver, Scope, ID, IPv4/IPv6 enabled flags, Internal, and CreatedAt.
- Local `wslc network list --format json` exposes ID, Name, Driver, Scope, IPv4/IPv6, Internal, Labels, and CreatedAt. `wslc network inspect bridge --format json` additionally returned Attachable and IPAM driver/config including IPv4 subnet and gateway. This was exercised against the built-in `bridge` network only.
- The inspected WSLc network did not have a Stack or Ownership field. Stack may be derivable from network labels in some cases, but that is not established as a general WSLc field; Ownership is Portainer-specific unless WSLc provides a corresponding source.

### Network details screen reference (2026-10-30)

- The user supplied a Portainer network-details screenshot with identity, driver/scope, attachability, internal status, IPAM subnet/gateway data, network options, and containers attached to the network. It also shows Access Control ownership, which is not a WSLc network property in the inspected output.
- Local `wslc network inspect bridge --format json` returns `Attachable`, `Internal`, `IPAM.Driver`, `IPAM.Config` (including subnet/gateway), `Options`, and a `Containers` object. A temporary nginx container inspection verified endpoint entries with name, endpoint ID, MAC, IPv4, and IPv6 fields; the temporary container was cleaned up.
- The Avalonia network-details screen displays inspect-backed fields, IPAM/options, and attached endpoint rows with container links; it offers Disconnect through `wslc network disconnect`. UI smoke testing covered the empty built-in bridge, while the service argument path is unit-tested; disconnecting a live endpoint was not integration-smoke-tested.

## Related-container groups and stacks (2026-10-30)

- The user likes grouping inter-related containers as a stack, but considers Portainer’s stack UI inadequate and wants a better experience. This is a product preference; the grouping model and orchestration behavior are not yet specified.
- The installed `wslc` CLI has no `compose` or `stack` command. Microsoft’s [WSL containers GA announcement](https://blogs.windows.com/windowsdeveloper/2026/09/29/wsl-containers-now-generally-available/) lists `wslc compose` as roadmap work. WSL Container Desktop describes its Compose support as orchestration above `wslc`, not a native CLI stack feature.
- Treat any future grouping/orchestration as an app-level capability unless WSLc later adds native Compose/stack commands; do not claim native support today.

## Compose module feasibility (2026-11-01)

- Decision: build an app-level "basic" Compose module above the `wslc` CLI and grow it, rather than waiting for Microsoft's native `wslc compose`. Native compose remains roadmap-only.
- The WSL Container Desktop reference proves the approach but is GPL-3.0, so we adapt its architecture (parse with MIT `YamlDotNet`, own Compose semantics) without copying code or assets.
- v1 boundary and handoff are in `research/compose-module-plan.md`.
## Useful questions for later discovery

These are research prompts, not decisions or requirements:

- Which of the user's container workflows and pain points should the UI optimize? Which tasks should remain CLI-first?
- What parts of the existing clients feel right or wrong to the user? Which specific features, interaction patterns, visual choices, or operational behaviors matter?
- Is the target exclusively WSLc, or should it also cover normal WSL distributions, Kubernetes, registries, or other engines?
- Should the app use the WSL container API, invoke `wslc.exe`, or use different integration surfaces for different operations? What are the version, packaging, lifecycle, and concurrency implications?
- What should happen around long-running sessions, startup/background operation, system tray behavior, errors, events, logs, terminals, mounts, ports, credential handling, and enterprise policy?
- If Compose matters, what compatibility level is required while native `wslc compose` remains on the roadmap?

## Sources

1. [Microsoft: WSL containers is now generally available](https://blogs.windows.com/windowsdeveloper/2026/09/29/wsl-containers-now-generally-available/) — official ecosystem and roadmap snapshot.
2. [WSL Container Desktop repository](https://github.com/mhackermsft/wslcontainerdesktop) — project description and README claims.
3. [WSLC Desktop repository](https://github.com/bacarndiaye/wslc-desktop) — project description and README claims.
4. [Microsoft WSL container overview](https://learn.microsoft.com/en-us/windows/wsl/wsl-container) — feature/API distinction.
