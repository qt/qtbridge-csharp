<!--
// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Resources in Qt Bridge for C# apps

Most production apps need non-code files such as images, fonts, text, JSON, icons, and other
content that ships with the app. In Qt Bridge for C# apps, these files are packaged into the
Qt Resource System and are addressed with `qrc:/` URLs at runtime.

The short version:

- Add files with `<QtResource Include="..." />`, or opt into `.resx` file-reference discovery with
  `QtBridgeResourceLibrary`.
- QML reads resources directly with `qrc:/...` URLs.
- C# reads the same packaged resources with `Qt.Resources`.
- Managed `.resx` embedding is not the default for Qt resources. Use `QtResourceAccess` only when a
  resource must also be available through .NET managed resource APIs.

## Resource model

Qt Bridge uses the native Qt resource store as the runtime source of truth. The canonical resource
URL is a `qrc:/` URL, and the default alias is based on the assembly that owns the resource:

```text
qrc:/assemblies/<AssemblyId>/<relative-path>
```

For example, add this to the project file (`.csproj`):

```xml
<ItemGroup>
  <QtResource Include="icons\app.svg" />
</ItemGroup>
```

is available at runtime as:

```text
qrc:/assemblies/MyApp/icons/app.svg
```

The default `AssemblyId` is the project assembly name. You can override it with
`QtBridgeAssemblyResourceId` when a stable resource namespace is needed.

Project file (`.csproj`):

```xml
<PropertyGroup>
  <QtBridgeAssemblyResourceId>MyCompany.Controls</QtBridgeAssemblyResourceId>
</PropertyGroup>
```

## Add file resources

Use `QtResource` for files that should be packaged as Qt resources.

Project file (`.csproj`):

```xml
<ItemGroup>
  <QtResource Include="icons\*" />
  <QtResource Include="data\catalog.json" />
  <QtResource Include="about.txt" />
</ItemGroup>
```

QML can use those resources directly:

QML file (`.qml`):

```qml
Image {
    source: "qrc:/assemblies/MyApp/icons/app.svg"
}
```

C# can read the same resource bytes through `Qt.Resources`:

C# file (`.cs`):

```csharp
if (Qt.Resources.Exists("qrc:/assemblies/MyApp/about.txt")) {
    string about = Qt.Resources.ReadAllText("qrc:/assemblies/MyApp/about.txt");
}

byte[] catalogBytes = Qt.Resources.ReadAllBytes(
    "qrc:/assemblies/MyApp/data/catalog.json");
```

For application icons, pass the same `qrc:/` URL to `Qt.Application.SetWindowIcon`:

C# file (`.cs`), usually near application startup:

```csharp
Qt.Application.SetWindowIcon("qrc:/assemblies/MyApp/icons/app.svg");
```

## Use `.resx` as a resource manifest

Qt Bridge can also discover file references from `.resx` files. This is useful when a project
already groups resource metadata in `.resx` files, but the runtime resource should still be the Qt
resource store.

Enable `.resx` discovery in the project:

Project file (`.csproj`):

```xml
<PropertyGroup>
  <QtBridgeResourceLibrary>true</QtBridgeResourceLibrary>
</PropertyGroup>
```

With this property enabled, Qt Bridge scans `*.resx` files for file-reference entries and packages
the referenced files as Qt resources. The file path, not the `.resx` key, determines the default
`qrc:/` alias. A file reference to `synopsis/history.txt` in `Books.resx` is therefore available as:

```text
qrc:/assemblies/Bookshelf/synopsis/history.txt
```

If a `.resx` file should stay managed-only and not participate in the Qt resource pipeline, remove
it from `QtResx`:

Project file (`.csproj`):

```xml
<ItemGroup>
  <QtResx Remove="App.resx" />
</ItemGroup>
```

## Choose an access mode

Each resource has an access mode:

| Mode | Qt `qrc:/` resource | Managed `.resx` resource |
|---|---:|---:|
| `Default` | yes | no |
| `ManagedAndNative` | yes | yes |
| `ManagedOnly` | no | yes |

`Default` is the normal mode. Use it for files that QML, Qt, or C# code reads through `qrc:/`.

Use `ManagedAndNative` only when the same `.resx` entry must also be reachable through managed
APIs such as `ResourceManager`. The override identity is `FileName.resx::ResourceName`:

Project file (`.csproj`):

```xml
<ItemGroup>
  <QtResourceAccess Include="Books.resx::SynopsisHistory"
    Mode="ManagedAndNative"
    Reason="Catalog export service reads synopsis via ResourceManager" />
</ItemGroup>
```

Managed embedding happens at `.resx` file granularity. If one entry in a `.resx` file is marked
`ManagedAndNative` or `ManagedOnly`, the whole `.resx` file is embedded into the managed assembly.

Use `ManagedOnly` for entries that should stay available to managed `.resx` APIs but must not be
used from QML through `qrc:/`. Build-time checks for accidental QML references to `ManagedOnly`
resources are not implemented yet, so use this mode deliberately.

## Override resource aliases

The default alias scheme avoids collisions across assemblies. Override an alias only when you need
compatibility with an existing Qt resource layout:

Project file (`.csproj`):

```xml
<ItemGroup>
  <QtResource Include="icons\*">
    <Alias>qt/qml/MyCompany.Controls/icons/%(Filename)%(Extension)</Alias>
  </QtResource>
</ItemGroup>
```

The resulting QML URL is:

```text
qrc:/qt/qml/MyCompany.Controls/icons/app.svg
```

If two resources claim the same alias for different physical files, the build fails with a
diagnostic instead of picking one silently.

## Use resources from referenced projects

Resources from referenced Qt Bridge projects are aggregated into the app build. A library can own
its resources, and the app can still read them by using the library assembly resource namespace:

QML file (`.qml`):

```qml
Image {
    source: "qrc:/assemblies/MyCompany.UiKit/icons/close.svg"
}
```

C# file (`.cs`):

```csharp
string text = Qt.Resources.ReadAllText(
    "qrc:/assemblies/MyCompany.UiKit/help/intro.html");
```

This is the recommended model for source-owned resource libraries.

## Localization and satellite assemblies

Qt Bridge resource aggregation is assembly-based, so resources declared by referenced Qt Bridge
projects are available to the app through their assembly resource namespace. This is useful for
shared UI libraries, icon packs, templates, and other source-owned resource projects.

.NET satellite resource assemblies, such as culture-specific `de/MyApp.resources.dll` or
`fr/MyApp.resources.dll` files, are not translated into Qt `qrc:/` resources by the current
resource pipeline. Keep culture-specific strings and other localized managed resources in the
normal .NET resource system and read them through `ResourceManager`.

C# file (`.cs`):

```csharp
using System.Globalization;
using System.Resources;

var resources = new ResourceManager("MyApp.Strings", typeof(Program).Assembly);
string title = resources.GetString("WindowTitle", CultureInfo.CurrentUICulture) ?? "My App";
Qt.Application.DisplayName = title;
```

When QML needs localized text, expose the localized value from C# as a property or method on your
QML-visible backend. For localized images or other files that QML must load with `qrc:/`, use a
convention in the Qt resource path, such as `i18n/de/help.html` or `images/fr/welcome.png`, and
choose the URL in C# or QML based on the current culture.

## Practical guidance

- Prefer `QtResource` for simple file resources.
- Prefer `QtBridgeResourceLibrary` when existing `.resx` file-reference entries are already the
  source of resource metadata.
- Keep UI-facing resources in the Qt resource store and refer to them with `qrc:/` from QML.
- Use `Qt.Resources.ReadAllText` and `Qt.Resources.ReadAllBytes` when C# needs the same packaged
  files.
- Avoid managed duplication unless a real managed API requires `ResourceManager` access.
- Keep default aliases unless you are integrating with an existing Qt resource path scheme.
- Use .NET `ResourceManager` for culture-specific satellite resources, and pass localized values
  from C# to QML when needed.

The `examples/Bookshelf` project demonstrates direct `QtResource` items, `.resx` file-reference
discovery, `ManagedAndNative`, `Qt.Resources.ReadAllText`, and `Qt.Application.SetWindowIcon`.
