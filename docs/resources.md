# Application Resources

Most applications need non-code files such as images, fonts, text, JSON, icons, and other content.
In Qt Bridge for C# applications, you package those files into the Qt resource store and use
`qrc:/` URLs to load them at runtime.

The short version:

* Add files with `<QtResource Include="..." />`.
* Use `qrc:/assemblies/<AssemblyId>/<relative-path>` from QML.
* Use `Qt.Resources` when C# code needs to read the same packaged files.
* Enable `.resx` discovery only when your project already uses `.resx` files as resource manifests.

All project file changes on this page go in your `.csproj` file.

## Understand the resource URL

Qt Bridge for C# gives each packaged file a `qrc:/` URL. The default URL includes the assembly
resource ID and the file path from your project:

```text
qrc:/assemblies/<AssemblyId>/<relative-path>
```

For a project named `MyApp`, this project item:

```xml
<ItemGroup>
    <QtResource Include="icons\app.svg" />
</ItemGroup>
```

is available at runtime as:

```text
qrc:/assemblies/MyApp/icons/app.svg
```

The default assembly resource ID is your project assembly name. If you need a stable namespace that
does not depend on the assembly name, set `QtBridgeAssemblyResourceId`:

```xml
<PropertyGroup>
    <QtBridgeAssemblyResourceId>MyCompany.Controls</QtBridgeAssemblyResourceId>
</PropertyGroup>
```

After that, the URL uses `MyCompany.Controls` in place of the assembly name.

## Add files

Use `<QtResource>` for files that QML, Qt, or C# code should read through `qrc:/`:

```xml
<ItemGroup>
    <QtResource Include="icons\*" />
    <QtResource Include="data\catalog.json" />
    <QtResource Include="about.txt" />
</ItemGroup>
```

You can include individual files or globs. Keep the default URL shape unless you are integrating
with an existing Qt resource layout.

## Use resources from QML

Reference the `qrc:/` URL wherever QML expects a URL:

```qml
Image {
    source: "qrc:/assemblies/MyApp/icons/app.svg"
}
```

Fonts use the same pattern with
[`FontLoader`](https://doc.qt.io/qt-6/qml-qtquick-fontloader.html):

```qml
FontLoader {
    source: "qrc:/assemblies/MyApp/fonts/Inter.ttf"
}
```

## Use resources from C#

Use [`Qt.Resources`](api/Qt.Resources.yml) when C# code needs the same packaged files:

```csharp
if (Qt.Resources.Exists("qrc:/assemblies/MyApp/about.txt"))
{
    string about = Qt.Resources.ReadAllText("qrc:/assemblies/MyApp/about.txt");
}

byte[] catalogBytes = Qt.Resources.ReadAllBytes(
    "qrc:/assemblies/MyApp/data/catalog.json");
```

Use [`ReadAllText`](api/Qt.Resources.yml) for text files and [`ReadAllBytes`](api/Qt.Resources.yml)
for binary files such as images, fonts, or custom data.

## Set the application icon

Package the icon with `<QtResource>`, then pass its `qrc:/` URL to
[`Qt.Application.SetWindowIcon`](api/Qt.Application.yml). Set it before you load QML:

```csharp
static void Main(string[] args)
{
    Qt.Application.SetWindowIcon("qrc:/assemblies/MyApp/icons/app.svg");
    Qml.LoadFromRootModule("Main");
    Qml.WaitForExit();
}
```

## Use `.resx` files as resource manifests

If your existing project already groups file references in `.resx` files, you can have Qt Bridge
for C# discover those file references and package the referenced files as Qt resources.

Enable `.resx` discovery in the project file:

```xml
<PropertyGroup>
    <QtBridgeResourceLibrary>true</QtBridgeResourceLibrary>
</PropertyGroup>
```

With this property enabled, the bridge scans `*.resx` files for file-reference entries. The file
path, not the `.resx` key, determines the default `qrc:/` URL. A file reference to
`synopsis/history.txt` in `Books.resx` is available as:

```text
qrc:/assemblies/Bookshelf/synopsis/history.txt
```

If a `.resx` file should stay managed-only and not participate in the Qt resource pipeline, remove
it from `QtResx`:

```xml
<ItemGroup>
    <QtResx Remove="App.resx" />
</ItemGroup>
```

## Choose an access mode

Each resource has an access mode:

| Mode | Qt `qrc:/` resource | Managed `.resx` resource |
|---|---:|---:|
| `Default` | Yes | No |
| `ManagedAndNative` | Yes | Yes |
| `ManagedOnly` | No | Yes |

`Default` is the normal mode. Use it for files that QML, Qt, or C# code reads through `qrc:/`.

Use `ManagedAndNative` only when the same `.resx` entry must also be available through managed APIs
such as
[`ResourceManager`](https://learn.microsoft.com/en-us/dotnet/api/system.resources.resourcemanager).
The override identity is `FileName.resx::ResourceName`:

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
used from QML through `qrc:/`. The current build does not check accidental QML references to
`ManagedOnly` resources, so use this mode deliberately.

## Override resource aliases

Override an alias only when you need compatibility with an existing Qt resource layout:

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

If two resources claim the same alias for different physical files, the build fails instead of
choosing one silently.

## Use resources from referenced projects

Resources from referenced Qt Bridge projects are aggregated into the app build. A library can own
its resources, and the app can still read them by using the library assembly resource namespace:

```qml
Image {
    source: "qrc:/assemblies/MyCompany.UiKit/icons/close.svg"
}
```

```csharp
string text = Qt.Resources.ReadAllText(
    "qrc:/assemblies/MyCompany.UiKit/help/intro.html");
```

Use this model for source-owned UI libraries, icon packs, templates, and other shared project
resources.

## Handle localized resources

.NET satellite resource assemblies, such as culture-specific `de/MyApp.resources.dll` or
`fr/MyApp.resources.dll` files, are not translated into Qt `qrc:/` resources by the current
resource pipeline. Keep culture-specific strings and other localized managed resources in the
normal .NET resource system and read them through
[`ResourceManager`](https://learn.microsoft.com/en-us/dotnet/api/system.resources.resourcemanager).

```csharp
using System.Globalization;
using System.Resources;

var resources = new ResourceManager("MyApp.Strings", typeof(Program).Assembly);
string title = resources.GetString("WindowTitle", CultureInfo.CurrentUICulture) ?? "My App";
Qt.Application.DisplayName = title;
```

When QML needs localized text, expose the localized value from C# as a property or method on your
QML-facing backend. For localized images or other files that QML must load with `qrc:/`, use a
path convention such as `i18n/de/help.html` or `images/fr/welcome.png`, then choose the URL from C#
or QML based on the current culture. Use
[`Qt.Application.DisplayName`](api/Qt.Application.yml) when the localized value should become the
application display name.

## Practical guidance

* Prefer `QtResource` for simple file resources.
* Prefer `QtBridgeResourceLibrary` when existing `.resx` file-reference entries are already the
  source of resource metadata.
* Keep UI-facing resources in the Qt resource store and refer to them with `qrc:/` from QML.
* Use [`Qt.Resources.ReadAllText`](api/Qt.Resources.yml) and
  [`Qt.Resources.ReadAllBytes`](api/Qt.Resources.yml) when C# needs the same packaged files.
* Avoid managed duplication unless a real managed API requires
  [`ResourceManager`](https://learn.microsoft.com/en-us/dotnet/api/system.resources.resourcemanager)
  access.
* Keep default aliases unless you are integrating with an existing Qt resource path scheme.
* Use .NET
  [`ResourceManager`](https://learn.microsoft.com/en-us/dotnet/api/system.resources.resourcemanager)
  for culture-specific satellite resources, and pass localized values from C# to QML when needed.

The [`examples/Bookshelf`](https://github.com/qt/qtbridge-csharp/tree/dev/examples/Bookshelf)
project demonstrates direct `QtResource` items, `.resx` file-reference discovery,
`ManagedAndNative`, [`Qt.Resources.ReadAllText`](api/Qt.Resources.yml), and
[`Qt.Application.SetWindowIcon`](api/Qt.Application.yml).

## Where to go from here

* See [Adding QML to Existing C# Projects](existing-csharp-projects.md) for the rest of the
  migration workflow.
* See the [API Reference](api-reference.md) for [`Qt.Resources`](api/Qt.Resources.yml) and
  [`Qt.Application`](api/Qt.Application.yml) details.
