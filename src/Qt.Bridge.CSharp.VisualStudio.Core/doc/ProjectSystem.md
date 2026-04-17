# ProjectSystem - Qt Bridge Project Detection

## Purpose

The `ProjectSystem` namespace answers one question on behalf of the Visual Studio extension:
**"Does this `.csproj` file belong to a Qt Bridge C# project?"**

It does so by reading the project file as plain XML - no MSBuild evaluation engine, no SDK
imports, no build execution. The result is fast, side-effect-free detection that works even
before a build has ever run.

---

## Design Principles

**Static XML analysis only.**
The detector parses the raw `.csproj` XML with `XDocument`. It never invokes the MSBuild
evaluation engine or loads the project into any IDE project system. This keeps detection
lightweight, safe to call on any thread, and free from the version-compatibility concerns
that come with MSBuild API usage.

**Conservative project file location.**
When given an arbitrary file or directory path the locator searches up the directory tree
looking for a `.csproj` file. If a directory contains more than one project file the locator
skips it and continues upward rather than guessing. Ambiguity is treated as "not found", not
as an error.

**Interface-driven composition.**
Every non-trivial component is hidden behind an interface (`IQtBridgeProjectFileLocator`,
`IQtBridgeProjectDetector`, `IQtBridgeProjectService`). Concrete types are `sealed`. This
keeps the seam clean for dependency injection and makes the components independently testable
without spinning up a Visual Studio process.

**Immutable, self-describing results.**
`QtBridgeProjectMetadata` is a read-only record that captures every indicator examined during
detection. Consumers can inspect exactly why a project was or was not classified as a Qt Bridge
project, and diagnostic output can be produced without repeating the analysis.

---

## Components

### `QtBridgeProjectConstants`

A static registry of the well-known identifiers that signal a Qt Bridge project:

| Category | Examples |
|---|---|
| NuGet package ID prefixes | `QtGroup.Qt.Bridge.CSharp.*` |
| Imported MSBuild files | `QtGroup.Qt.Bridge.CSharp.props`, `Qt.Bridge.targets`, … |
| MSBuild property names | `QtDotNetPropsImported`, `QtQmlRootModule`, `QtDir`, … |

Helper methods (`IsKnownQtBridgePackageId`, `IsKnownImportedFile`) centralise the matching
logic so the rest of the code never hard-codes string comparisons.

---

### `IQtBridgeProjectFileLocator` / `QtBridgeProjectFileLocator`

Answers: **"Which `.csproj` file owns this path?"**

Given a file path or directory, `FindEnclosingProjectFile` searches upward through parent
directories. It returns the path of the single `.csproj` found in the first directory that
contains exactly one, or `null` if the search reaches the root without finding an unambiguous
match.

---

### `IQtBridgeProjectDetector` / `QtBridgeProjectDetector`

Answers: **"Is this `.csproj` a Qt Bridge project?"**

`DetectAsync` loads the project file as XML and checks three independent indicators in a
single pass:

1. **PackageReference** - a `<PackageReference Include="…">` whose `Include` value starts
   with a known Qt Bridge NuGet package ID prefix.
2. **Imported files** - one or more `<Import Project="…">` elements whose path contains a
   known Qt Bridge `.props` or `.targets` file name.
3. **MSBuild properties** - a non-empty `<PropertyGroup>` child element whose name matches
   a known Qt Bridge property (e.g. `QtQmlRootModule`, `QtDir`).

A project is classified as `QtBridgeCSharp` if **any one** indicator is present. The file I/O
is dispatched to the thread pool via `Task.Run` so the calling thread is never blocked.

---

### `QtBridgeProjectMetadata`

An immutable result type returned by the detector. It records:

- The resolved absolute path to the project file.
- The `QtBridgeProjectType` classification (`Unknown` or `QtBridgeCSharp`).
- The `bool` flag `IsQtBridgeProject` for quick tests.
- Which specific indicators matched (`MatchedPackageId`, `ImportsQtBridgeProps`,
  `ImportsQtBridgeTargets`).
- The values of any known MSBuild properties found in the file (`Properties` dictionary).

---

### `QtBridgeProjectType`

A simple enum with two members:

| Value | Meaning |
|---|---|
| `Unknown` | The project was not recognised as a Qt Bridge project. |
| `QtBridgeCSharp` | A C# project that uses the Qt Bridge for C# NuGet package. |

---

### `IQtBridgeProjectService` / `QtBridgeProjectService`

The top-level entry point for extension code. It composes the locator and the detector into a
single async method:

```
TryGetMetadataForPathAsync(path)
  -> locator.FindNearestProjectFile(path)   // locate the .csproj
  -> detector.DetectAsync(projectFilePath)  // analyse it
  -> QtBridgeProjectMetadata?               // null when no project file found
```

Any component in the extension that needs to know whether a file belongs to a Qt Bridge
project only depends on `IQtBridgeProjectService`.

---

### `QtBridgeProjectSummaryFormatter`

A diagnostic helper that renders a `QtBridgeProjectMetadata` object as a human-readable
multi-line string. Intended for log output and debug windows.

---

## Typical Call Flow

```
Extension receives a document path (e.g. from the active editor)
  │
IQtBridgeProjectService.TryGetMetadataForPathAsync(documentPath)
  │
  |- IQtBridgeProjectFileLocator.FindNearestProjectFile(documentPath)
  │    Search parent directories -> returns path to .csproj, or null
  │
  |- IQtBridgeProjectDetector.DetectAsync(projectFilePath)
       Parse XML -> check PackageReference / Import / PropertyGroup
       -> QtBridgeProjectMetadata
```

If `TryGetMetadataForPathAsync` returns `null`, no project file was found. If it returns a
`QtBridgeProjectMetadata` with `IsQtBridgeProject == false`, a project file exists but does
not carry any Qt Bridge indicator.
