# Metadata-based Exports (Experimental)

By default, Qt Bridge generates C++ wrappers for C# types that QML uses. The experimental
metadata-based export mode describes those types in JSON instead. The bridge reads that metadata
at runtime, so a project can build without compiling generated C++ wrappers.

This mode is still under active development. Its behavior and supported scenarios may change.

## Try it with a new project

The project template has an experimental option that enables metadata exports for the project:

```bash
dotnet new qt -n MyQtApp --ExportAsMetadata
```

The option is also available in the project template wizards in VS Code and Visual Studio. Select
**Export types as metadata (experimental)** when creating the project.

## Try it in an existing project

Set the project-wide default in the `.csproj` file:

```xml
<PropertyGroup>
    <QtExportAs>metadata</QtExportAs>
</PropertyGroup>
```

Then build the project as usual. The generator writes type information to
`obj/<configuration>/<framework>/qt/native/qt_bridge_metadata.json` instead of writing a separate
C++ wrapper for each exported type.

The project property sets the default export mode. Individual types or assemblies can override it
with [`[Qt.Export(Options = Qt.ExportAs.Metadata)]`](api/Qt.ExportAttribute.yml) (see also
[`Qt.ExportAs.Metadata`](api/Qt.ExportAs.yml)) or
[`[Qt.Export(Options = Qt.ExportAs.SourceCode)]`](api/Qt.ExportAttribute.yml) (see also
[`Qt.ExportAs.SourceCode`](api/Qt.ExportAs.yml)). If any exported types use source-code export,
those types still need generated C++ wrappers and a native C++ build.

## Avoiding C++ compilation

For a build without generated C++ wrappers:

* Export all C# types used by QML as metadata. The built-in mode is still source-code export for
  types without another configured default.
* Keep [`QtResourcePackaging`](resources.md#choose-resource-packaging) at its default,
  [`Auto`](resources.md#choose-resource-packaging), or set it to
  [`External`](resources.md#choose-resource-packaging). Do not select
  [`Linked`](resources.md#choose-resource-packaging): linking Qt resources into the native host
  requires a C++ build.

In metadata-only mode, [`Auto`](resources.md#choose-resource-packaging) uses an external resource
pack when needed. The pack uses Qt's resource compiler, but does not require compiling generated
C++ wrappers. See [Application Resources](resources.md) for details.

The bridge still uses its native runtime and application host. This mode avoids compiling generated
type wrappers; it does not remove the native runtime from the application.

## Current limitation

Metadata-only deployment is currently supported on Windows. On Linux and macOS the build warns
that deployment is not supported in this mode, so use Windows to try a complete metadata-only
application workflow for now.

## What to explore

Metadata exports cover common QML-facing types and members, including properties, methods, events,
indexers, models, and collection-as-model types. They also enable load-time type registration and
dynamic-object dispatch. Try your application and report cases where metadata mode behaves
differently from the default source-code mode.
