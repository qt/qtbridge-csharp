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
  |- QmlLanguageServer/
  │    |- QmlLanguageServerProvider    <- LanguageServerProvider (VS SDK)
  │    │    |- IExtensionLog           <- all provider diagnostics
  │    │    |- INotificationService    <- install / launch / metadata errors
  │    │    |- IProjectContextService  <- active doc, project, config
  │    │    |- IQtBridgeProjectService <- Qt Bridge project detection
  │    │    |- IQmlMetadataReader      <- reads qtbridge-qml.ide.json
  │    │    |- IQmlMetadataWatcher     <- polls for metadata file changes
  │    │    |- IQmlLanguageServerInstaller <- download/cache qmlls binary
  │    │
  │    |- QmlLanguageServerTransportPipe
  │    │                               <- IDuplexPipe wrapping the qmlls process
  │    │    |- IExtensionLog           <- transport relay diagnostics
  │    |- LspByteBuffer                <- LSP frame/body extraction helper
  │    |- Contracts/Lsp.cs             <- LSP notification/request DTOs
  │    |- Contracts/ProjectAssets.cs   <- project.assets.json DTOs
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
is not required at startup. Before launch the provider resolves import paths from two sources:
the Qt Bridge NuGet package's `tools/qt/qml` directory (located via `project.assets.json`
without requiring a build) and the import path lists from any projects that have already been
built. This ensures standard Qt modules such as `QtQuick` resolve from the moment the server
starts, even before the first build. Per-project `workspace/didChangeWorkspaceFolders` and
`$/addBuildDirs` notifications are then sent after the LSP handshake completes and again
whenever the metadata file changes. This removes the hard dependency on metadata being present
at launch and allows the server to cover multiple projects in one session without restarting.

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

**`.qmlls.build.ini` is patched with a project-source-root alias before each injection.**
qmlls selects build settings for an open file by matching the file path against `.qmlls.build.ini`
section headers using a `startsWith` check. The section headers are keyed by the native source
root (`obj/.../qt/native/source`), which never matches user-authored QML files that live under
the project root. Before enqueuing `$/addBuildDirs` for a project, the provider calls
`TryPatchQmllsBuildIni`, which appends an alias section to each `.qt/.qmlls.build.ini` file
found in the project's build directories. The alias section uses `projectSourceDir` as the key
and copies the `importPaths` and `resourceFiles` values from the native section. The patch
checks for the alias header before writing and is a no-op if the alias already exists.

**Injection is deferred until build output is complete.**
After a build, `qtbridge-qml.ide.json` may appear before `.qmlls.build.ini` and generated
`.qmltypes` files are ready. Injecting at that point would let qmlls cache incomplete build
settings in the running session. Instead, `TryInjectProjectAsync` checks two readiness
conditions before proceeding: `TryPatchQmllsBuildIni` must succeed (ini file present and
native section found) and `TryGeneratedQmlTypesReady` must confirm that every generated
import path exists and contains at least one readable `.qmltypes` file. If either check
fails, the injection is deferred: `EnsureBuildSettingsWatcher` starts a `FileSystemWatcher`
on the project directory and retries `TryInjectProjectAsync` on each file-system event until
both conditions are met.

**Metadata change defers restart until build output is ready.**
When the metadata watcher fires, the provider sets `RestartWhenIniReady = true` on the
project entry and calls `TryInjectProjectAsync` rather than restarting immediately.
`TryInjectProjectAsync` runs the two readiness checks above; once both pass it detects the
`RestartWhenIniReady` flag, clears it, and calls `RestartServerForProjectAsync` to toggle
`Enabled` with a 500 ms gap. The restart ensures the fresh process patches `.qmlls.build.ini`
and reads it without any cached state, so all types resolve correctly from the first document
request of the new session.

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

## QML Language Server Feature Folder

The qmlls integration lives under `QmlLanguageServer/`. The folder groups the Visual Studio
language-server provider, the transport pipe, protocol helpers, and serializer contracts so
the extension root stays limited to composition and cross-cutting extension services.

`Contracts/Lsp.cs` contains the DTOs used to serialize LSP notifications and injected
requests. `Contracts/ProjectAssets.cs` contains only the subset of `project.assets.json`
needed to locate the Qt Bridge package's `tools/qt/qml` directory. These are implementation
contracts for serialization, not domain models.

`LspByteBuffer` is a feature-local helper that accumulates raw stream bytes and extracts
complete LSP messages. The transport uses it for outbound message forwarding.

---

### `QmlLanguageServerProvider`

The central piece of the extension. Extends `LanguageServerProvider` and manages the full
lifecycle of the QML Language Server:

**Per-project registry.**
The provider maintains a `Dictionary<string, ProjectEntry>` (keyed by project file path)
protected by a lock. Each `ProjectEntry` holds an `IDisposable` watcher, the project
directory, the config key, and four state fields:
- `BuildDirsInjected` - `true` once workspace and build-dir notifications have been sent
  for this project in the current server session. Prevents duplicate injections when multiple
  code paths race to register the same project. Reset to `false` at the start of each new
  server session.
- `MissingMetadataNotified` - `true` once the "build project X for full QML support" InfoBar
  has been shown for this project. Prevents the banner from repeating on every poll tick.
- `RestartWhenIniReady` - set by `OnProjectMetadataChanged` to signal that the server should
  be restarted once the build output is fully ready. Cleared by `TryInjectProjectAsync` when
  it detects all readiness checks pass.
- `IniWatcher` - holds the `FileSystemWatcher` that retries injection while waiting for
  `.qmlls.build.ini` or generated `.qmltypes` files to appear. Disposed and nulled once
  injection succeeds or the entry is displaced by a config change.

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
2. Collect import paths from two sources across all loaded Qt Bridge projects, deduplicated:
   - **NuGet package path**: reads `obj/project.assets.json` for each project, locates the
     Qt Bridge package entry in the `libraries` map, resolves it via `packageFolders`, and
     returns `<folder>/<path>/tools/qt/qml` if that directory exists. This works without a
     prior build and ensures built-in Qt types resolve immediately.
   - **Built metadata paths**: reads `qtbridge-qml.ide.json` for each project that has one
     and adds its `ImportPaths` list.
   Each resolved path is logged at `Info` level; a "no paths found" entry is logged if the
   scan produces nothing.
3. Resolve the active project context (directory, project file path, config key).
4. Launch the qmlls process with `--no-cmake-calls` and the collected import paths. A
   `QmlLanguageServerLaunchException` is caught, an error notification is shown, and the
   method returns `null`. On success, stores the pipe as `activePipe`.
5. Reset `BuildDirsInjected` on all existing registry entries (they belong to the previous
   server session) and re-inject workspace/build-dir context for every previously registered
   project, then register and inject the active project.

**`TryInjectProjectAsync`.**
Reads metadata for a registered project and, if valid, runs two readiness checks before
sending any notifications:

1. `TryPatchQmllsBuildIni` - returns `false` if the `.qt/.qmlls.build.ini` file does not
   yet exist in any build directory, or if the native section header has not appeared yet.
2. `TryGeneratedQmlTypesReady` - returns `false` if any generated import path (one whose
   location is under a build directory) does not exist on disk, contains no `.qmltypes`
   files, or has a `.qmltypes` file that cannot be opened for reading (still being written).

If either check fails, injection is reset and `EnsureBuildSettingsWatcher` is called to
start a `FileSystemWatcher` on the project directory. The watcher re-queues
`TryInjectProjectAsync` on `Created`, `Changed`, or `Renamed` events until both checks
pass. Once both pass and `RestartWhenIniReady` is set, the method clears the flag and calls
`RestartServerForProjectAsync` instead of injecting into the current session. Otherwise it
enqueues `workspace/didChangeWorkspaceFolders` and `$/addBuildDirs` on the active pipe.

If no metadata file exists, a "build project X" InfoBar is shown once (controlled by
`MissingMetadataNotified`). If the active pipe changed while awaiting async work,
`BuildDirsInjected` is reset so the next session retries. On successful injection,
`QueueSemanticTokensRefresh` is called to prompt VS to re-classify open documents.

**`QueueSemanticTokensRefresh`.**
qmlls does not send a `workspace/semanticTokens/refresh` request to VS after it receives new
build directory or workspace information, so already-open documents keep stale token
classification until the next editor event. This helper works around that by queuing a 250 ms
delayed call to `pipe.EnqueueSemanticTokensRefresh()`, triggered after both server
initialization and each successful build-directory injection.

**`OnProjectMetadataChanged`.**
Called by each project's `IQmlMetadataWatcher` when the metadata file timestamp changes.
Resets `BuildDirsInjected`, sets `RestartWhenIniReady`, disposes any existing `IniWatcher`,
and calls `TryInjectProjectAsync`. The actual server restart is deferred until
`TryInjectProjectAsync` confirms that `.qmlls.build.ini` is patched and all generated
`.qmltypes` files are readable. If `Enabled` is already `false`, calls
`RefreshEnabledStateAsync` to re-attempt activation instead.

---

### `QmlLanguageServerTransportPipe`

Implements `IDuplexPipe` by wrapping the qmlls `Process`. It owns two background tasks, two
`System.IO.Pipelines.Pipe` instances, two unbounded `Channel<byte[]>` queues, and a
`TaskCompletionSource<bool>` (`serverInitializedSource`) that coordinates the relay tasks
around the LSP handshake.

**`EnqueueNotification(json)`** queues a notification for delivery to qmlls (VS → process
direction). The notification is held until after `initialized` arrives, then delivered without
waiting for VS to send additional traffic.

**`EnqueueSemanticTokensRefresh()`** queues an injected `workspace/semanticTokens/refresh`
request in the process → VS direction (`pendingServerRequests` channel). The request carries
a unique `qtbridge-semanticTokens-refresh-N` ID that is tracked in `injectedRequestIds`.
When VS responds, `IsInjectedRequestResponse` detects the matching ID and drops the response
so qmlls never sees an unsolicited reply. VS may also echo a `NotificationReceived`
notification back; `IsVsRefreshEchoNotification` detects and drops that too.

**`BuildWorkspaceFolderNotification(folderUri, add)`** and
**`BuildAddBuildDirsNotification(folderUri, buildDirs)`** are internal static helpers that
serialize the respective notification DTOs to JSON using `DataContractJsonSerializer`.
The provider calls these and hands the results to `EnqueueNotification`.

**`RelayFromProcessAsync`** reads qmlls stdout and forwards bytes to the VS-facing read pipe.
Before `serverInitializedSource` is signalled, the relay holds a pending `source.ReadAsync`
but races it against the TCS task so it can detect initialization without blocking. After
initialization, it additionally races against `pendingServerRequests`: whenever the channel
has items and qmlls has not sent output yet, the relay writes the synthetic requests directly
into the VS-facing pipe. It also parses all messages through `LspByteBuffer` for diagnostic
logging (`Verbose` level).

**`RelayToProcessAsync`** reads from the VS-facing write pipe and forwards bytes to qmlls
stdin. It sets `handshakeComplete` and signals `serverInitializedSource` when the
`initialized` notification passes through. Before forwarding each message it calls
`IsInjectedRequestResponse` and `IsVsRefreshEchoNotification` and drops matching messages.
After the handshake it enters a select loop that drains `pendingNotifications` (the
extension-to-qmlls notification queue) without waiting for VS to send a message.

**`LspByteBuffer`** is a feature-local helper that accumulates raw bytes from an LSP stream and
extracts complete framed messages (`Content-Length: N\r\n\r\n<body>`). Both relay tasks use
it - the from-process task for diagnostic logging, the to-process task to detect the
`initialized` notification and to log outbound methods. `TryExtractBody` is a static helper
used by the response-filtering methods to reach the JSON body without re-parsing the header.

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
       |- TryFindImportPathsAsync()  -> NuGet tools/qt/qml + built metadata import paths
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
  -> set RestartWhenIniReady
  -> TryInjectProjectAsync()
       waits until .qmlls.build.ini is patched and generated .qmltypes files are readable
  -> Enabled = false  (VS SDK shuts down qmlls)
  -> await Task.Delay(500)
  -> Enabled = true   (VS SDK calls CreateServerConnectionAsync again)
       -> CreateServerConnectionAsync re-injects all registry entries with fresh metadata

Case B: user opens a .qml file in a project not yet registered
  -> CreateServerConnectionAsync detects project not in registry
  -> EnsureProjectRegisteredAsync() starts a new watcher for that project
  -> TryInjectProjectAsync() sends workspace + build-dir notifications to running server
```
