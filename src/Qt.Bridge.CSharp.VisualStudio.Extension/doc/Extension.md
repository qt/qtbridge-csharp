# Qt Bridge C# Visual Studio Extension

## Purpose

This project is the Visual Studio extension that activates QML Language Server support for
Qt Bridge C# projects. It is built on the new Visual Studio Extensibility SDK
(`Microsoft.VisualStudio.Extensibility`) and hosts the language server, manages its lifecycle,
and wires together the detection, installation, and metadata components from the Core library.

The extension is the thin integration layer. All domain logic - project detection, language
server installation, and metadata file handling - lives in the Core library and is consumed
here through the DI container.

---

## Architecture Overview

```
ExtensionEntrypoint (DI root)
  │
  |- IExtensionLog / ExtensionLog       <- dual-output logging (TraceSource + output pane)
  |- INotificationService / NotificationService  <- rate-limited VS InfoBar messages
  │
  |- QmlLanguageServerProvider         <- LanguageServerProvider (VS SDK)
  │    |- IExtensionLog                <- all provider diagnostics
  │    |- INotificationService         <- user-facing install / launch / metadata errors
  │    |- IProjectContextService       <- VS IDE context (active doc, project, config)
  │    |- IQtBridgeProjectService      <- Qt Bridge project detection
  │    |- IQmlMetadataReader           <- reads qtbridge-qml.ide.json
  │    |- IQmlMetadataWatcher          <- polls for metadata file changes
  │    |- IQmlLanguageServerInstaller  <- download/cache qmlls binary
  │
  |- QmlLanguageServerTransportPipe    <- IDuplexPipe wrapping the qmlls process
  │    |- IExtensionLog                <- transport relay diagnostics
  │
  |- QmlMetadataWatcher (Extension)    <- IQmlMetadataWatcher implementation
  │    |- IExtensionLog                <- watcher error reporting
  │
  |- QtBridgeStatusCommand             <- Extensions menu diagnostic command
  │
  |- DteProjectContextService          <- DTE-backed IProjectContextService
```

---

## Design Principles

**The extension is the composition root, not the logic owner.**
`ExtensionEntrypoint` registers all Core library services as singletons and nothing more.
No business logic lives in the extension layer - it delegates everything to the Core
interfaces. This keeps the extension thin and the Core library independently testable.

**Logging is centralised behind `IExtensionLog`.**
All extension components receive an `IExtensionLog` through the DI container rather than
depending directly on `TraceSource` or the VS output channel API. `ExtensionLog` writes
`Verbose` entries to the `TraceSource` only (captured by the VS diagnostics log) and
`Info`, `Warning`, and `Error` entries to both the `TraceSource` and the "Qt Bridge for C#"
VS output channel so developers see important events without opening the diagnostics log.
This dual-output approach is also the reason the `IQmlMetadataWatcher` implementation lives
in the extension rather than in Core: the watcher needs to report errors through
`IExtensionLog`, and `IExtensionLog` carries a dependency on Visual Studio output
infrastructure that must not leak into the Core library.

**`Enabled` is the lifecycle switch.**
The VS Extensibility SDK activates and deactivates a `LanguageServerProvider` by reading its
`Enabled` property. The provider sets `Enabled = true` when at least one Qt Bridge project is
loaded, and `Enabled = false` otherwise. To restart the server (e.g., after a build produces
new metadata), the provider briefly sets `Enabled = false`, waits 500 ms for the SDK to shut
down the current connection, then sets `Enabled = true` again to trigger a fresh
`CreateServerConnectionAsync` call.

**The server starts unconditionally; metadata is injected after initialization.**
The server is launched as soon as any Qt Bridge project is detected in the solution. Metadata
is not required at startup: if no project has been built yet the server starts with
`--no-cmake-calls` and whatever import paths can be found from any already-built project.
Per-project `workspace/didChangeWorkspaceFolders` and `$/addBuildDirs` notifications are
sent after the LSP handshake completes and again whenever the metadata file changes.
This removes the hard dependency on metadata being present at launch and allows the server
to cover multiple projects in one session without restarting.

**Active document takes precedence over selected project.**
When `CreateServerConnectionAsync` resolves which project to configure the server for, it
checks the active document first. If a document is open and it does not belong to a Qt Bridge
project, the method returns `null` immediately - the selected project is never consulted.
This prevents the provider from starting a server configured for a Qt Bridge project when
the active document belongs to a different project type that also uses `.qml` files.
The selected project is only used as a fallback when no document is open (e.g., when setting
up the metadata watcher from Solution Explorer).

**Per-project workspace registration covers the full solution.**
After the LSP handshake the provider sends two notifications per project:
`workspace/didChangeWorkspaceFolders` registers the project source root as a workspace
folder so qmlls tracks all `.qml` files under it, and `$/addBuildDirs` maps that source root
to the Qt-native build directories where `.qt/.qmlls.build.ini` lives. Together these give
qmlls coverage of both generated and user-authored QML files. The transport pipe delivers
these via an `EnqueueNotification` channel that is drained immediately after `initialized`
arrives - without waiting for VS to send another message - so the server is fully configured
before the first document request.

**Metadata change triggers a server restart, not re-injection.**
When the watcher detects that a project's `qtbridge-qml.ide.json` has changed (e.g. after a
build), the provider restarts the server by toggling `Enabled` with a 500 ms gap rather than
injecting updated notifications into the running session. This is necessary because qmlls
memoizes `.qmlls.build.ini` reads: re-sending `$/addBuildDirs` cannot update import paths or
resource file lists already cached in the running process. A restart lets
`CreateServerConnectionAsync` register the workspace with fresh data before any `didOpen`.

**DTE is abstracted behind an interface.**
All Visual Studio IDE state - active project, active document, active build configuration,
platform, loaded projects, file ownership - is accessed through `IProjectContextService`.
The concrete implementation (`DteProjectContextService`) uses the DTE automation API.
This keeps `QmlLanguageServerProvider` free of DTE references and replaceable with
SDK-native equivalents as the VS Extensibility SDK adds new capabilities over time.

**Context changes are debounced.**
DTE fires multiple events in quick succession when a solution opens, closes, or a document
is switched. `DteContextSubscription` runs all events through a 250 ms debounce timer so
`RefreshEnabledStateAsync` is invoked once per burst, not dozens of times.

**In-process hosting is required.**
`RequiresInProcessHosting = true` is set on the extension configuration because the
implementation needs `ThreadHelper.JoinableTaskFactory` (to marshal between background
threads and the VS main thread) and DTE (which is only available in-process). Out-of-process
hosting cannot satisfy either dependency.

---

## Components

### `ExtensionEntrypoint`

The extension entry point. Subclasses `Microsoft.VisualStudio.Extensibility.Extension` and
overrides `InitializeServices` to register all services as singletons. This is the only place
where concrete types are bound to their interfaces. The registration order reflects the
dependency graph: `IExtensionLog` and `INotificationService` are registered first as they are
consumed by several other services, including the extension-layer `QmlMetadataWatcher`.

---

### `IExtensionLog` / `ExtensionLog`

A lightweight logging abstraction with four severity levels: `Verbose`, `Info`, `Warning`,
and `Error` (the last optionally accepting an `Exception`). All extension components receive
this through the DI container.

`ExtensionLog` is the only implementation. It writes to two outputs simultaneously:

- **`TraceSource`** - all four severity levels. The VS Extensibility SDK injects the
  `TraceSource`; `ExtensionLog` adds a `DefaultTraceListener`, sets the switch level to
  `Verbose`, and maps each severity to the corresponding `TraceEventType`. These entries
  appear in the VS diagnostics log.
- **VS output channel** ("Qt Bridge for C#") - `Info`, `Warning`, and `Error` only.
  `Verbose` entries are not forwarded here to keep routine diagnostics out of the pane
  visible to users.

Exception details are appended to the message string in both outputs when present.

---

### `INotificationService` / `NotificationService`

A rate-limited InfoBar notification service. `ShowInfoAsync`, `ShowWarningAsync` and
`ShowErrorAsync` each accept a string `key` and a message. `NotificationService` maintains
a `HashSet<string>` of previously shown keys; if the same key is presented a second time,
the InfoBar is not shown again for the lifetime of the extension session. This prevents the
same error (e.g. a missing asset or a failed installation) from spawning repeated InfoBar
banners across server restart cycles.

Each notification is also forwarded to `IExtensionLog` at the corresponding severity level
so the event is captured in the diagnostics log even if the user dismisses the InfoBar.

---

### `QmlMetadata/QmlMetadataWatcher`

The extension-layer implementation of `IQmlMetadataWatcher` (the interface is defined in
Core). It takes `IExtensionLog` as a constructor parameter and adds two capabilities over
what a Core-only implementation could provide:

- **Callback errors are caught and logged.** If the `OnMetadataChanged` callback throws, the
  exception is logged via `IExtensionLog.Error` instead of silently terminating the poll loop.
- **Timestamp-read failures are logged with deduplication.** If `FindMetadataFilePath` or the
  file-system read fails, the error is logged once. Subsequent poll ticks that produce the
  same error are suppressed to avoid flooding the log every two seconds.

---

### `QmlDocumentTypes`

A static `[VisualStudioContribution]` that registers the `qml` document type for `.qml`
files, derived from `LanguageServerBaseDocumentType`. This registration is what allows
`QmlLanguageServerProvider` to declare a `DocumentFilter` so VS routes `.qml` documents
to this provider.

---

### `QmlLanguageServerProvider`

The central piece of the extension. Extends `LanguageServerProvider` and manages the full
lifecycle of the QML Language Server:

**Per-project registry.**
The provider maintains a `Dictionary<string, ProjectEntry>` (keyed by project file path)
protected by a lock. Each `ProjectEntry` holds an `IDisposable` watcher, the project
directory, the config key, and two state flags:
- `BuildDirsInjected` - `true` once workspace and build-dir notifications have been sent
  for this project in the current server session. Prevents duplicate injections when multiple
  code paths race to register the same project. Reset to `false` at the start of each new
  server session.
- `MissingMetadataNotified` - `true` once the "build project X for full QML support" InfoBar
  has been shown for this project. Prevents the banner from repeating on every poll tick.

**Enabled-state management.**
On construction, and whenever the VS context changes (project selection, document open,
solution load/close), `RefreshEnabledStateAsync` is called. It checks whether any loaded
project is a Qt Bridge project and sets `Enabled` accordingly. When enabling, it immediately
registers the active project and all other loaded Qt Bridge projects via
`EnsureProjectRegisteredAsync`, which starts per-project metadata watchers and triggers
`TryInjectProjectAsync` for any project that has not yet been injected.

**`CreateServerConnectionAsync`.**
Called by the VS SDK when `Enabled` is `true`. The sequence is:

1. Ensure qmlls is installed via `IQmlLanguageServerInstaller`. A
   `QmlLanguageServerInstallException` is caught, logged with its typed `Error` kind, and
   a per-error-kind InfoBar error is shown via `INotificationService` (keyed so the same
   failure is not re-shown on restart). The method returns `null` on any install failure.
2. Collect import paths best-effort: scan all loaded Qt Bridge projects and return the import
   paths from the first one that has valid built metadata. These paths are identical for all
   projects on the same NuGet version, so any project serves as a valid source.
3. Resolve the active project context (directory, project file path, config key).
4. Launch the qmlls process with `--no-cmake-calls` and the collected import paths. A
   `QmlLanguageServerLaunchException` is caught, an error notification is shown, and the
   method returns `null`. On success, stores the pipe as `activePipe`.
5. Reset `BuildDirsInjected` on all existing registry entries (they belong to the previous
   server session) and re-inject workspace/build-dir context for every previously registered
   project, then register and inject the active project.

**`TryInjectProjectAsync`.**
Reads metadata for a registered project and, if valid, enqueues two notifications on the
active pipe: `workspace/didChangeWorkspaceFolders` (adds the project source root as a
workspace folder) and `$/addBuildDirs` (maps the source root to the Qt-native build
directories). If no metadata file exists, a "build project X" InfoBar is shown once
(controlled by `MissingMetadataNotified`). If the active pipe changed while awaiting async
work, `BuildDirsInjected` is reset so the next session retries.

**`OnProjectMetadataChanged`.**
Called by each project's `IQmlMetadataWatcher` when the metadata file timestamp changes.
Resets `BuildDirsInjected` for that project and restarts the server by toggling `Enabled`
(with a 500 ms gap). Restart is used rather than re-injection because qmlls memoizes
`.qmlls.build.ini` reads; only a fresh process picks up new import paths and resource files.
If `Enabled` is already `false`, calls `RefreshEnabledStateAsync` to re-attempt activation.

---

### `QmlLanguageServerTransportPipe`

Implements `IDuplexPipe` by wrapping the qmlls `Process`. It owns two background tasks, two
`System.IO.Pipelines.Pipe` instances, and an unbounded `Channel<byte[]>` for pending
out-of-band notifications.

**`EnqueueNotification(json)`** is the public entry point for injecting notifications. The
caller passes a raw JSON body; the pipe applies LSP framing (`Content-Length` header) and
writes the bytes to the channel. The relay task drains the channel after `initialized`
arrives without waiting for VS to send additional traffic.

**`BuildWorkspaceFolderNotification(folderUri, add)`** and
**`BuildAddBuildDirsNotification(folderUri, buildDirs)`** are internal static helpers that
serialize the respective notification DTOs to JSON using `DataContractJsonSerializer`.
The provider calls these and hands the results to `EnqueueNotification`.

**`RelayFromProcessAsync`** reads qmlls stdout and forwards bytes to the VS-facing read pipe.
It also parses incoming messages through `LspByteBuffer` for diagnostic logging only
(method names and byte counts are logged at `Verbose` level).

**`RelayToProcessAsync`** reads from the VS-facing write pipe and forwards bytes to qmlls
stdin. After the `initialized` notification passes through, the relay enters a select loop
that races the VS read task against the pending notifications channel: whenever the channel
has items and VS has not sent a new message yet, the relay drains and flushes the queued
notifications immediately. This ensures per-project workspace registration happens without
requiring VS to generate LSP traffic.

**`LspByteBuffer`** is a private helper that accumulates raw bytes from an LSP stream and
extracts complete framed messages (`Content-Length: N\r\n\r\n<body>`). Both relay tasks use
it - the from-process task for diagnostic logging, the to-process task to detect the
`initialized` notification and to log outbound methods.

`Dispose` cancels both relay tasks, waits up to 500 ms (via `JoinableTaskFactory.Run` to
avoid deadlocking the VS main thread), kills the process if it has not exited, and releases
all resources.

---

### `QtBridgeStatusCommand`

A diagnostic `Command` placed in the Extensions menu. When invoked, it resolves the active
project or selected item, calls `IQtBridgeProjectService.TryGetMetadataForPathAsync`, and
displays the formatted `QtBridgeProjectMetadata` summary (from `QtBridgeProjectSummaryFormatter`)
in a VS prompt dialog. If no Qt Bridge project context is found it shows a plain "not found"
message. Intended for developer diagnostics, not end-user workflows.

---

### `IProjectContextService` / `DteProjectContextService`

`IProjectContextService` defines the contract for querying VS IDE state:

| Method | Returns |
|---|---|
| `GetActiveProjectPathAsync` | Path of the project selected in Solution Explorer |
| `GetActiveDocumentPathAsync` | Path of the currently open document |
| `GetActiveConfigurationAsync` | Active solution build configuration name (e.g. `Debug`) |
| `GetActivePlatformAsync` | Active solution platform name (e.g. `x64`, `Any CPU`) |
| `GetOwningProjectPathAsync` | Project that owns a given file path |
| `GetLoadedProjectPathsAsync` | All project paths loaded in the current solution |
| `SubscribeToContextChanged` | Event subscription with debounce; returns `IDisposable` |

`DteProjectContextService` implements this using the DTE2 automation API. All DTE calls
require the VS main thread (`SwitchToMainThreadAsync` or `ThrowIfNotOnUIThread`).

`DteContextSubscription` (private nested class) subscribes to DTE `SolutionEvents`
(opened, closed, project added/removed/renamed) and `DocumentEvents` (document opened).
All events are funnelled through a 250 ms `Timer`-based debounce before invoking the caller's
callback. This prevents rapid re-evaluation when VS fires multiple events for a single
user action.

> **Note:** The DTE dependency is a known limitation of the current VS Extensibility SDK. The
> code is structured to make it straightforward to replace DTE-backed methods with SDK-native
> equivalents as they become available. See the `TODO` remarks in `DteProjectContextService`
> for the specific tracking issues.

---

## Typical Activation Flow

```
VS loads solution containing one or more Qt Bridge projects
  │
DteContextSubscription fires (solution opened)
  -> debounce 250 ms
  -> RefreshEnabledStateAsync()
  │
  |- ShouldEnableForActiveContextAsync()
  │    checks active project / document / loaded projects via IQtBridgeProjectService
  │    -> true
  │
  |- EnsureProjectRegisteredAsync() for active project and all loaded Qt Bridge projects
  │    IQmlMetadataWatcher.Watch(projectDirectory, configKey, OnProjectMetadataChanged)
  │    TryInjectProjectAsync() [no-op: activePipe is still null]
  │
  |- Enabled = true  (VS SDK calls CreateServerConnectionAsync)
       │
       |- IQmlLanguageServerInstaller.EnsureInstalledAsync()  -> executable path
       |- TryFindImportPathsAsync()  -> import paths from any built project (best-effort)
       |- ResolveActiveProjectContextAsync()  -> (dir, projectFile, configKey)
       │
       |- LaunchQmlLanguageServer()
       │    Process.Start(qmlls, --no-cmake-calls [-I ...])
       │    -> QmlLanguageServerTransportPipe (IDuplexPipe)
       │         RelayFromProcessAsync  [background]
       │         RelayToProcessAsync    [background]
       │
       |- activePipe = pipe; reset BuildDirsInjected on all registry entries
       │
       |- TryInjectProjectAsync() for each previously registered project
       |- EnsureProjectRegisteredAsync() for active project
            TryInjectProjectAsync():
              FindMetadataFilePath() -> read -> validate
                success: EnqueueNotification(workspace/didChangeWorkspaceFolders)
                          EnqueueNotification($/addBuildDirs)
                missing: ShowInfoAsync("build project X for full QML support") [once]
```

```
LSP handshake completes (VS sends 'initialized')
  │
RelayToProcessAsync detects 'initialized'
  -> drains pendingNotifications channel immediately
       -> sends workspace/didChangeWorkspaceFolders to qmlls stdin
       -> sends $/addBuildDirs to qmlls stdin
  -> enters select loop: drains channel whenever VS is not sending a message
```

```
Developer builds the project (or a second project is visited)
  │
Case A: metadata file changes for a registered project
  QmlMetadataWatcher poll fires (up to 2 s later)
  -> OnProjectMetadataChanged()
  -> reset BuildDirsInjected for that project
  -> Enabled = false  (VS SDK shuts down qmlls)
  -> await Task.Delay(500)
  -> Enabled = true   (VS SDK calls CreateServerConnectionAsync again)
       -> CreateServerConnectionAsync re-injects all registry entries with fresh metadata

Case B: user opens a .qml file in a project not yet registered
  -> CreateServerConnectionAsync detects project not in registry
  -> EnsureProjectRegisteredAsync() starts a new watcher for that project
  -> TryInjectProjectAsync() sends workspace + build-dir notifications to running server
```
