# QmlMetadata - QML IDE Metadata File Handling

## Purpose

The `QmlMetadata` namespace is responsible for locating, reading, validating, and watching
the `qtbridge-qml.ide.json` file that the Qt Bridge MSBuild target writes into a project's
`obj\` directory after each build. The extension reads this file to learn how to start and
configure the QML Language Server for the active project.

---

## The MSBuild Producer

The `qtbridge-qml.ide.json` file is written by the `QtBridgeWriteQmlMetadataFile` target in
`build/Qt.Bridge.targets`. Several of its implementation choices directly explain the
behaviour of the reader and watcher on the extension side.

**Timing: `AfterTargets="PrepareForBuild"`.**
The target runs early - before C# compilation - because all the values it needs are evaluated
MSBuild properties (`IntermediateOutputPath`, `Configuration`, `Platform`, `QtDir`, etc.).
No compiler outputs are required. The target is skipped entirely during design-time builds
(`DesignTimeBuild == 'true'`) so it does not slow down IntelliSense evaluation.

**Output path matches the reader's location logic.**
The file is written to `obj\Configuration\` for Any CPU builds and to
`obj\Platform\Configuration\` for named platforms (x64, arm64, etc.). MSBuild uses `AnyCPU`
internally while Visual Studio passes `Any CPU` (with a space) as the platform name - the
target normalises both to the unqualified `obj\Configuration\` layout. This is exactly the
path structure that `FindMetadataFilePath` searches for.

**Directories are pre-created before the file is written.**
The target calls `MakeDir` to create `SourceDir`, `SourceDir\qml`, and `BuildDirs[0]` before
writing the metadata file. This is intentional: the extension's `Validate` call checks that
these directories exist on disk. Without pre-creation, the first watcher notification would
arrive when the metadata file appears but before `QtBridgeGenerate` and `QtBridgeBuild` have
populated those directories - validation would fail and the language server would never start
unless the user manually reopened the QML file after the build completed.

**`WriteOnlyWhenDifferent` prevents spurious watcher notifications.**
The metadata content changes only when the project configuration changes (e.g. switching from
Debug to Release). Using `WriteOnlyWhenDifferent="true"` ensures the file's last-write
timestamp is not bumped on a clean rebuild where nothing has changed, avoiding unnecessary
watcher callbacks and server restarts in the extension.

**Import paths suppress the qmlls CI fallback.**
The target always includes the module output directory and the generated QML source root as
import paths. When `$(QtDir)` is set it also appends `$(QtDir)\qml`. Passing these paths via
`-I` to qmlls suppresses the CI-baked fallback path (`D:/a/qmlls-workflow/...`) compiled into
the qmlls binary, which would otherwise be used when no explicit import paths are provided.

---

## Design Principles

**Read and validate are separate operations.**
`TryRead` deserializes the JSON file without applying any semantic checks. `Validate` performs
those checks as a distinct step. This separation lets callers decide when validation is
appropriate - for example, reading once to check the version field before committing to full
validation, or re-validating a previously cached result when the project configuration
changes.

**Conservative file location.**
`FindMetadataFilePath` never guesses when the result is ambiguous. If a bare configuration
name (e.g. `Debug`) matches metadata files under more than one platform directory (e.g.
`obj\x64\Debug\` and `obj\arm64\Debug\`), the method returns `null` rather than picking one
silently. Callers should supply a platform-qualified key (e.g. `x64\Debug`) when the active
platform is known.

**Polling instead of `FileSystemWatcher`.**
A .NET build generates hundreds of file-system events in the `obj\` tree. On Windows, this
volume routinely overflows the kernel buffer that backs `FileSystemWatcher`, causing the
watcher to silently drop events - including the creation of the metadata file itself.
The watcher implementation avoids this by polling every two seconds, checking only the
last-write timestamp of the metadata file. This is deliberately low-tech: the file appears at
most once per build, so a two-second delay is acceptable and the approach is completely
reliable.

**DTO layer isolates JSON from the public model.**
`QmlMetadataReader` deserializes JSON into a set of private `DataContract` DTO classes and
then maps them to the public `QmlMetadata` model. This keeps `[DataMember]` attributes and
serialization concerns out of the domain model and lets the mapping code enforce required
fields and filter blank entries before a `QmlMetadata` object is ever constructed. The DTO
layer also handles backward compatibility: the legacy `qmlls` JSON key is accepted alongside
the current `qmlLanguageServer` key, with the canonical key taking precedence.

**Interface-driven composition.**
`IQmlMetadataReader` and `IQmlMetadataWatcher` define the contracts. Concrete types are
`sealed`. Both are straightforward to stub in tests without standing up a build or a file
system.

**The watcher implementation lives in the extension layer, not Core.**
`IQmlMetadataWatcher` is defined in Core so that the provider can depend on it through the
DI container, but the concrete implementation resides in the extension project. This split
exists because the watcher needs to report errors through `IExtensionLog` - a logging
abstraction that carries a dependency on Visual Studio's `TraceSource` infrastructure. Keeping
that dependency out of Core preserves Core's independence from any IDE or UI layer.

---

## Components

### `QmlMetadata`

An immutable model of the `qtbridge-qml.ide.json` file. It carries two nested sections:

**`QmlSection`** - directory information the language server needs:

| Property | Description |
|---|---|
| `SourceDir` | The generated Qt-native source root used as the primary qmlls workspace. Matches the section key written into `.qt/.qmlls.build.ini`. |
| `ProjectSourceDir` | The user's original project source root. Used for the runtime `$/addBuildDirs` mapping so qmlls covers user-authored QML files, not just generated ones. |
| `BuildDirs` | One or more Qt-native build directories containing `.qt/.qmlls.build.ini`. |
| `ImportPaths` | Additional QML import paths passed to qmlls via `-I`. These suppress the CI-baked fallback path compiled into the qmlls binary. |

**`QmlLanguageServerSection`** - startup policy:

| Property | Description |
|---|---|
| `DisableCMakeCalls` | Whether to launch qmlls with `--no-cmake-calls`. Defaults to `true` for Qt Bridge C# projects, which do not use CMake. |

---

### `IQmlMetadataReader` / `QmlMetadataReader`

Three operations, intended to be called in sequence:

**`FindMetadataFilePath(projectDirectory, configKey)`**
Searches `obj\` under the project directory for a `qtbridge-qml.ide.json` file whose
containing directory path ends with `configKey`. First tries the exact canonical path
`obj\<configKey>\qtbridge-qml.ide.json` - this handles Any CPU builds (which write
`obj\Debug\`) correctly even when a stale platform-qualified file exists alongside it.
Falls back to a recursive tail-match for layouts where `BaseIntermediateOutputPath` adds
extra segments. Returns `null` if zero or more than one match is found.

**`TryRead(metadataFilePath)`**
Opens and deserializes the file using `DataContractJsonSerializer`. All failures -
file not found, I/O errors, malformed JSON - return `null`. No exceptions are propagated.

**`Validate(metadata, projectFilePath, configuration)`**
Applies semantic checks to a deserialized `QmlMetadata` object:
- Schema version must be `1`.
- `ProjectFile` must resolve to the same path as `projectFilePath`.
- `Configuration` must match `configuration`.
- `SourceDir` must exist on disk.
- `BuildDirs` must be non-empty and every entry must exist on disk.

Returns `false` for any failure. A `false` result means the file should be treated as
missing or stale and the extension should wait for the next build.

---

### `IQmlMetadataWatcher`

Defines the contract for watching the metadata file. `Watch(projectDirectory, configuration,
callback)` returns an `IDisposable` - disposing it stops the watcher. The concrete
implementation lives in the extension project (see the Extension documentation) so that
watcher errors can be reported through `IExtensionLog` without introducing a UI dependency
into Core.

---

## Typical Call Flow

```
Build completes - MSBuild writes qtbridge-qml.ide.json
  │
IQmlMetadataWatcher callback fires (after next 2-second poll tick)
  │
IQmlMetadataReader.FindMetadataFilePath(projectDirectory, configKey)
  -> resolves path to qtbridge-qml.ide.json under obj\
  │
IQmlMetadataReader.TryRead(metadataFilePath)
  -> deserializes JSON to QmlMetadata
  │
IQmlMetadataReader.Validate(metadata, projectFilePath, configuration)
  -> confirms version, project match, dirs exist
  │
  |- false: treat as missing, keep waiting
  |- true: hand QmlMetadata to the QML Language Server startup logic
```
