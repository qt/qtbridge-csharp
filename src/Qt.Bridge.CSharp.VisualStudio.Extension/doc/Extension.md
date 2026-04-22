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
  |- IExtensionLog / TraceSourceExtensionLog  <- centralised logging abstraction
  │
  |- QmlLanguageServerProvider         <- LanguageServerProvider (VS SDK)
  │    |- IExtensionLog                <- all provider diagnostics
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
depending directly on `TraceSource`. This makes the call sites uniform and keeps the
`TraceSource` setup and listener configuration in one place (`TraceSourceExtensionLog`).
It is also the reason the `IQmlMetadataWatcher` implementation lives in the extension rather
than in Core: the watcher needs to report errors, which requires `IExtensionLog`, and
`IExtensionLog` carries a dependency on Visual Studio's tracing infrastructure that must not
leak into the Core library.

**`Enabled` is the lifecycle switch.**
The VS Extensibility SDK activates and deactivates a `LanguageServerProvider` by reading its
`Enabled` property. The provider sets `Enabled = true` when at least one Qt Bridge project is
loaded, and `Enabled = false` otherwise. To restart the server (e.g., after a build produces
new metadata), the provider briefly sets `Enabled = false`, waits 500 ms for the SDK to shut
down the current connection, then sets `Enabled = true` again to trigger a fresh
`CreateServerConnectionAsync` call.

**Minimal mode bridges the gap before the first build.**
The QML Language Server can start before the project has ever been built - no metadata file
exists yet. In this case the provider starts qmlls with only `--no-cmake-calls` (minimal
mode) so the editor is not left without any QML support. When the metadata file appears after
the first build the watcher fires, the `minimalMode` flag ensures the restart is logged
clearly, and the server is restarted with full arguments.

**Active document takes precedence over selected project.**
When `CreateServerConnectionAsync` resolves which project to configure the server for, it
checks the active document first. If a document is open and it does not belong to a Qt Bridge
project, the method returns `null` immediately - the selected project is never consulted.
This prevents the provider from starting a server configured for a Qt Bridge project when
the active document belongs to a different project type that also uses `.qml` files.
The selected project is only used as a fallback when no document is open (e.g., when setting
up the metadata watcher from Solution Explorer).

**`$/addBuildDirs` extends qmlls coverage to user-authored files.**
qmlls reads workspace configuration from `.qt/.qmlls.build.ini` in the Qt-native build
directory, which covers the generated source tree. It does not automatically know about the
user's original project source directory. The transport pipe intercepts the `initialized`
notification sent by VS after the LSP handshake completes and immediately injects a
`$/addBuildDirs` notification, mapping the project source root to the Qt-native build
directories. This is what makes qmlls aware of user-authored `.qml` files.

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
dependency graph: `IExtensionLog` is registered first as it is consumed by several other
services, including the extension-layer `QmlMetadataWatcher`.

---

### `IExtensionLog` / `TraceSourceExtensionLog`

A lightweight logging abstraction with four severity levels: `Verbose`, `Info`, `Warning`,
and `Error` (the last optionally accepting an `Exception`). All extension components receive
this through the DI container.

`TraceSourceExtensionLog` is the only implementation. It wraps the `TraceSource` that the VS
Extensibility SDK injects, adds a `DefaultTraceListener`, sets the switch level to `Verbose`,
and maps each severity to the corresponding `TraceEventType`. Exception details are appended
to the message string when present.

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

**Enabled-state management.**
On construction, and whenever the VS context changes (project selection, document open,
solution load/close), `RefreshEnabledStateAsync` is called. It checks whether any loaded
project is a Qt Bridge project and sets `Enabled` accordingly. When enabling, it also
starts the metadata file watcher so subsequent builds trigger server restarts automatically.

**`CreateServerConnectionAsync`.**
Called by the VS SDK when a `.qml` file is opened and `Enabled` is `true`. The sequence is:

1. Resolve the active project context (directory, project file path, config key).
2. Ensure qmlls is installed via `IQmlLanguageServerInstaller`.
3. Locate the metadata file. If absent, start in minimal mode; if present, read and validate
   it, then start with full arguments.
4. Launch the qmlls process and return a `QmlLanguageServerTransportPipe` as the `IDuplexPipe`.

**Metadata watcher and restart.**
After enabling, the provider starts an `IQmlMetadataWatcher` on the active project's `obj\`
directory. When the watcher fires (`OnMetadataChanged`), the provider restarts the server
by toggling `Enabled`. If the server was not running at all (e.g., first startup was in
minimal mode or cancelled before metadata existed), it calls `RefreshEnabledStateAsync`
instead to re-attempt activation from scratch.

---

### `QmlLanguageServerTransportPipe`

Implements `IDuplexPipe` by wrapping the qmlls `Process`. It owns two background tasks and
two `System.IO.Pipelines.Pipe` instances:

**`RelayFromProcessAsync`** reads qmlls stdout and writes to the VS-facing read pipe,
forwarding LSP responses and notifications to the VS LSP host.

**`RelayToProcessAsync`** reads from the VS-facing write pipe and writes to qmlls stdin,
forwarding LSP requests and notifications from VS to qmlls. It also handles
`$/addBuildDirs` injection: when it sees the `initialized` notification pass through
(the VS SDK sends this immediately after the LSP handshake), it injects the pre-built
`$/addBuildDirs` notification before forwarding any further messages.

**`LspByteBuffer`** is a private helper that accumulates raw bytes from an LSP stream and
extracts complete framed messages (`Content-Length: N\r\n\r\n<body>`). It is used by both
relay tasks - the from-process task uses it for diagnostic logging; the to-process task
uses it to detect the `initialized` notification.

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
VS loads solution containing a Qt Bridge project
  │
DteContextSubscription fires (solution opened)
  -> debounce 250 ms
  -> RefreshEnabledStateAsync()
  │
  |- ShouldEnableForActiveContextAsync()
  │    checks active project / document / loaded projects via IQtBridgeProjectService
  │    -> true
  │
  |- UpdateMetadataWatcherAsync()
  │    IQmlMetadataWatcher.Watch(projectDirectory, configKey, OnMetadataChanged)
  │
  |- Enabled = true  (VS SDK calls CreateServerConnectionAsync)
       │
       |- ResolveActiveProjectContextAsync()  -> (dir, projectFile, configKey)
       |- IQmlLanguageServerInstaller.EnsureInstalledAsync()  -> executable path
       |- IQmlMetadataReader.FindMetadataFilePath()
       │    found -> TryRead -> Validate
       │    not found -> minimal mode (--no-cmake-calls only)
       │
       |- LaunchQmlLanguageServer()
            Process.Start(qmlls, args)
            -> QmlLanguageServerTransportPipe (IDuplexPipe)
                 RelayFromProcessAsync  [background]
                 RelayToProcessAsync    [background]
                   on 'initialized': inject $/addBuildDirs
```

```
Developer builds the project
  │
MSBuild writes qtbridge-qml.ide.json
  │
QmlMetadataWatcher poll fires (up to 2 s later)
  -> OnMetadataChanged()
  -> Enabled = false  (VS SDK shuts down qmlls)
  -> await Task.Delay(500)
  -> Enabled = true   (VS SDK calls CreateServerConnectionAsync again)
       -> full metadata this time -> qmlls starts with -b / -I args
```
