# Compose module — v1 plan

Handoff for a cheaper model. Implement this in the existing `WslcContainerAdmin` Avalonia app.

## Objective

Add app-level Docker Compose orchestration **above the `wslc` CLI**. No native `wslc compose` exists (Microsoft roadmap). We own parsing, ordering, and reconciliation.

## Hard constraints (repo conventions)

- **MVVM**: all state/actions in ViewModels and services. Views' `.axaml.cs` contain only `InitializeComponent()`. No event handlers or interaction logic in code-behind.
- **`wslc.exe` only**: no `Microsoft.WSL.Containers` SDK. Argument-safe calls via existing `IWslcCli.RunAsync(arguments, ct)` (uses `ArgumentList`).
- **Unit tests only** in the test project: `FakeCli` (records `LastArguments`, returns queued outputs) and fake service contracts. Never launch `wslc.exe` in the unit suite.
- Tests live in `src/WslcContainerAdmin.Tests`; solution is `src/WslcContainerAdmin.sln`.
- Dark palette/panels already defined in `App.axaml`; reuse `Classes="panel"`, `sectionTitle`, `tableHeader`, `muted`, and the `← …` Back pattern from existing detail screens.

## v1 scope

**In:**

- Single `compose.yaml` (one document). No `include`, `extends`, or override merging.
- `services`: `image`, `container_name`, `command`, `entrypoint`, `environment` (map or `KEY=VALUE` list), `env_file`, `ports` (short `host:container` and `container` forms), `volumes` (short `volume:/path`), `networks`, `labels`, `working_dir`, `user`, `mem_limit`, `cpus`, `shm_size`, `depends_on` (start order only).
- Top-level `networks` (`driver`), top-level `volumes`, optional top-level `name` (project name).
- Interpolation: `${VAR}` and `${VAR:-default}` from process environment and sibling `.env`.
- `up` (create/start in dependency order) and `down` (stop/remove in reverse order, optional volume/network removal).

**Out (defer — parsed leniently, ignored where it makes sense):**

Recognized-but-unsupported keys are **silently ignored** so stock example `compose.yaml` files load unchanged: `restart`/`restart_policy`, secrets/configs, profiles, replicas/`deploy`, `include`/`extends`, multi-file override merge, health-gated `depends_on` conditions (keep the ordering, drop the condition), port ranges, `tmpfs`/`ulimit`, `cap_add`/`cap_drop`, `devices`, `healthcheck`, `build` (only when an `image` is also present — `build`-only services have nothing to run and remain a hard error).

Unknown top-level/service keys are ignored too. **Do not silently ignore** what changes the result: a missing `image` with no other source, a service that fails to parse into a runnable config, unresolved required `${VAR}`, or malformed YAML/port/volume syntax — those still error.

## Dependency

Add **`YamlDotNet` 16.3.0** (MIT) to `src/WslcContainerAdmin/WslcContainerAdmin.csproj`. Use it only for YAML *syntax*; implement Compose semantics (interpolation, normalization, ordering) in our own code. It has no transitive dependencies and targets `net8.0`/`netstandard2.x`, compatible with the app's `net10.0-windows10.0.19041.0`.

## New files

```
src/WslcContainerAdmin/Models/ComposeProject.cs
src/WslcContainerAdmin/Services/Compose/IComposeParser.cs
src/WslcContainerAdmin/Services/Compose/ComposeParser.cs
src/WslcContainerAdmin/Services/IComposeService.cs
src/WslcContainerAdmin/Services/ComposeService.cs
src/WslcContainerAdmin/ViewModels/ComposeViewModel.cs
src/WslcContainerAdmin/Views/ComposeView.axaml
src/WslcContainerAdmin/Views/ComposeView.axaml.cs          (InitializeComponent only)
src/WslcContainerAdmin.Tests/ComposeParserTests.cs
src/WslcContainerAdmin.Tests/ComposeServiceTests.cs
```

## Models (`Models/ComposeProject.cs`)

```csharp
public sealed record ComposeProject(
    string Name,
    IReadOnlyList<ComposeService> Services,
    IReadOnlyDictionary<string, ComposeNetwork> Networks,
    IReadOnlyDictionary<string, ComposeVolume> Volumes);

public sealed record ComposeService(
    string Name,
    string? ContainerName,
    string Image,
    string? Command,
    IReadOnlyList<string> Entrypoint,
    IReadOnlyList<string> Environment,          // "KEY=VALUE"
    IReadOnlyList<string> EnvFiles,
    IReadOnlyList<PortMapping> PublishedPorts,  // reuse Models.PortMapping
    IReadOnlyList<VolumeMount> VolumeMounts,    // reuse Models.VolumeMount
    IReadOnlyList<string> Networks,
    IReadOnlyList<string> Labels,               // "KEY=VALUE"
    string? WorkingDirectory,
    string? User,
    string? MemoryLimit,
    string? CpuLimit,
    string? SharedMemorySize,
    IReadOnlyList<string> DependsOn);

public sealed record ComposeNetwork(string Driver = "bridge");
public sealed record ComposeVolume(string Name);
```

## CLI mapping (`Services/ComposeService.cs`)

| Compose field | wslc arguments |
|---|---|
| project + service name | `--name {project}-{service}` |
| `image` | positional after options |
| `command` / `entrypoint` | `--entrypoint` + positional command (reuse `ContainerRunOptions`) |
| `environment` | `--env KEY=VALUE` |
| `env_file` | `--env-file path` |
| `ports` | `--publish host:container` |
| `volumes` | `--volume volume:/path` |
| `networks` | `--network name` (first) |
| `labels` | `--label KEY=VALUE` |
| `mem_limit` / `cpus` / `shm_size` | `--memory` / `--cpus` / `--shm-size` |
| `working_dir` / `user` | `--workdir` / `--user` |
| top-level networks | `network create --driver {driver} {project}-{name}` |
| top-level volumes | `volume create {project}-{name}` |
| pull | `image pull {image}` (only if not already listed) |

Reuse `ContainerRunOptions` for service → run construction. Its fields already cover these (see `Models/ContainerRunOptions.cs`). There is no `--restart`, so `restart` is out of scope.

## Service contract (`Services/IComposeService.cs`)

```csharp
public interface IComposeService
{
    Task UpAsync(ComposeProject project, CancellationToken cancellationToken = default);
    Task DownAsync(ComposeProject project, bool removeVolumes = false, CancellationToken cancellationToken = default);
}
```

`ComposeService` dependencies: `IWslcCli`, `IWslcContainerService`, `IWslcInventoryService`, `IWslcImageService`. It shells network/volume create through `IWslcCli` directly (no dedicated network/volume service yet) and uses existing services for run/list/remove/pull.

`UpAsync` steps:
1. Create declared networks, then volumes (idempotent: treat "already exists" errors as success where the CLI returns nonzero — verify the CLI's actual behavior first).
2. Pull images not present in `IWslcImageService.ListAsync()`.
3. Topologically order services by `depends_on`; reject cycles.
4. For each service in order, map to `ContainerRunOptions` with `RunInBackground = true` and `Name = "{project}-{service}"`, call `IWslcContainerService.RunAsync`.

`DownAsync` steps:
1. List containers, select those whose `Name` starts with `{project}-`.
2. Stop/remove in reverse dependency order.
3. If `removeVolumes`, remove `{project}-{name}` volumes via `IWslcInventoryService.RemoveVolumeAsync`.

## Parser (`Services/Compose/ComposeParser.cs`)

```csharp
public interface IComposeParser
{
    ComposeProject Parse(string yaml, IReadOnlyDictionary<string, string> environment, string? baseDirectory = null);
}
```

- Use `YamlDotNet` (a `Deserializer` into `Dictionary<object,object>` or a restricted typed model). Reject unsupported YAML tags/malformed documents with a clear `ComposeConfigurationException` carrying no raw file contents or paths.
- Normalize list/map forms of `environment`, `ports`, `volumes`, `networks`, `labels`, `depends_on`.
- Interpolate `${VAR}` and `${VAR:-default}` on string values only; load `.env` from `baseDirectory` for defaults. Leave `$$` as a literal `$`. Reject missing required variables.
- Enforce bounds (reject files over, say, 1 MB; ≤ 64 nesting) to stay safe.

## ViewModel / View

`ComposeViewModel`:
- Observable: `ProjectPath`, `ComposeProject? Project`, `IsLoading`, `ErrorMessage`, `StatusMessage`.
- `[RelayCommand]` `LoadAsync` (read file, parse, show service list), `UpAsync`, `DownAsync`, `Cancel` (back to containers or previous list via `INavigationService`).
- Use `Microsoft.Extensions.DependencyInjection` for `IComposeService`/`IComposeParser`.

`ComposeView.axaml`: a panel with a project-path `TextBox` + `Load`/`Up`/`Down`/`Cancel` buttons, an error/status area, and a service list (name/image/ports) reusing the table styles. No file-picker button in v1 (paste the path).

## Integration (2 edits)

- `WslcContainerAdminServiceCollectionExtensions.AddWslcContainerAdminServices`: register `IComposeParser` → `ComposeParser`, `IComposeService` → `ComposeService`, and `ComposeViewModel` as transient.
- `MainWindow.axaml`: add a `DataTemplate x:DataType="vm:ComposeViewModel"` → `views:ComposeView`. Optionally add a `NavigationItem("Compose", typeof(ComposeViewModel))` in `MainViewModel`.

## Tests

- `ComposeParserTests`: environment normalization (map vs list), port/volume short-form parsing, interpolation (`${VAR}`, default, `$$`, missing-required), `depends_on` ordering, cycle rejection.
- `ComposeServiceTests` (with `FakeCli`): assert the exact argument sequences for networks/volumes/run/pull and that `DownAsync` removes in reverse order and only gated volumes.

Do **not** write tests that assert source text or trivial forwarding.

## Acceptance criteria

1. `dotnet build src/WslcContainerAdmin.sln` clean; all tests pass.
2. A minimal `compose.yaml` (e.g. one `hello-world` service, one named volume, one bridge network) parses, `UpAsync` produces correct `wslc` calls in order, and `DownAsync` produces reverse removal.
3. `ComposeView` renders under the existing shell and navigation; no code-behind logic.
4. Unsupported fields (`restart`, `secrets`, `profiles`, `build` alongside `image`, etc.) are silently ignored and a stock example file loads unchanged; only structurally-required missing values (no runnable `image`) or malformed input error out.

## Later passes (candidates to explore, not v1)

The user wants to revisit some deferred features after v1. Promising leads:

- **`profiles`** — likely cheap and app-level. A profiled service is simply not started unless its profile is selected; no new `wslc` primitive needed, just parser tracking + a profile selection on `up`.
- **`secrets` / `configs`** — WSLc has no native secret store, but GA added `--mount` on `create`/`run`. Candidate: stage secret/config file content, then emit `--mount type=bind,src=<staged>,dst=/run/secrets/<name>,ro`. Verify actual `--mount` syntax against `wslc container run --help` before relying on it.
- **`restart`** — no `--restart` flag; only viable as app-level supervision (the app must stay running), matching the WSL Container Desktop approach. Evaluate cost/benefit separately.

Do not implement any of these in v1; record findings in the research note when explored.

## Verify before yielding

Run the thing: create a temp two-service compose fixture, drive `UpAsync`/`DownAsync` through a throwaway script or the running UI, and clean up any containers/volumes/networks created. Record live `wslc network create`/`volume create` exit behavior (idempotency) in the research note before relying on "already exists" handling.
