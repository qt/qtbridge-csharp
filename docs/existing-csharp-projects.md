# Adding QML to Existing C# Projects

Adding Qt Bridge for C# to an existing project means making a few decisions that a fresh template
project handles for you: package selection, startup shape, exposing existing C# types to QML, and
packaging application resources.

If you are starting from scratch, see [Getting Started](getting-started.md),
[C# and QML](csharp-and-qml.md) and [Project Templates](templates-and-examples.md) first.

## Requirements

Before you add the bridge to an existing project, confirm that the project meets these requirements:

* .NET SDK 8+
* Windows x64 or Linux x64
* CMake and Ninja available on `PATH`
* A C++ toolchain (Visual Studio Build Tools on Windows, `build-essential` on Linux)
* On Linux: a Qt 6 installation whose prefix contains `lib/cmake/Qt6/Qt6Config.cmake`

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not
  need to write CMake files or C++ code yourself.
</p>

## Add the package

Choose the package that matches your target platform.

### Visual Studio

Open the NuGet Package Manager for your project (**Tools > NuGet Package Manager > Manage NuGet
Packages for Solution**) and search for `QtGroup.Qt.Bridge.CSharp`. Install the package that
matches your target platform:

* `QtGroup.Qt.Bridge.CSharp.win-x64` for Windows x64
* `QtGroup.Qt.Bridge.CSharp.linux-x64` for Linux x64

### .NET CLI

From your project folder, run:

```bash
# Windows
dotnet add package QtGroup.Qt.Bridge.CSharp.win-x64 --version 0.3.*-*

# Linux
dotnet add package QtGroup.Qt.Bridge.CSharp.linux-x64 --version 0.3.*-*
```

The package is platform-specific because the bridge includes a native integration layer for the
target platform. The `0.3.*-*` version range stays on the 0.3 pre-release train.

## Update the entry point

Your `Main` method needs two calls to start and run the QML side:

```csharp
using Qt.Quick;

static void Main(string[] args)
{
    Qml.LoadFromRootModule("Main");
    Qml.WaitForExit();
}
```

* [`Qml.LoadFromRootModule("Main")`](api/Qt.Quick.Qml.yml) loads `Main.qml` as the root QML
  component and creates the window it describes. The string must match the QML file name without
  the `.qml` extension.
* [`Qml.WaitForExit()`](api/Qt.Quick.Qml.yml) blocks `Main` until the QML engine shuts down,
  keeping the process alive while the UI runs.

If your application has background work to do while the UI is running, use a finite timeout and
loop:

```csharp
Qml.LoadFromRootModule("Main");
while (!Qml.WaitForExit(100))
{
    // update data, poll queues, or tick background state
}
```

## Add QML files

Add at least one `.qml` file to the project directory. The build picks up every `.qml` file in
the project tree automatically — no project file entry is needed. The name you pass to
`LoadFromRootModule` must match the file name without the `.qml` extension.

For a minimal starting point, `Main.qml` can be as simple as:

```qml
import QtQuick
import QtQuick.Controls

ApplicationWindow {
    visible: true
    width: 640
    height: 480
    title: "My App"
}
```

## Build

On Windows, build with:

```bash
dotnet build
dotnet run
```

On Linux, point `QtDir` at your Qt installation:

```bash
dotnet build -p:QtDir=/path/to/qt-prefix
dotnet run -p:QtDir=/path/to/qt-prefix
```

The first build produces the bridge information and native support needed to make your C# types
visible to QML.

## Expose existing C# types to QML

By default, all `public` types defined in your project are exposed to QML after the first
successful build. You do not need to opt types in one by one.

QML uses `camelCase` for property names and method names, while your C# code keeps `PascalCase`.
This mapping is automatic; a property named `UserName` in C# is `userName` on the QML side.

For a C# type to update the QML side when its state changes, implement
[`INotifyPropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged).
QML treats a notifying property as a live binding source: whenever your code raises the change
event, every QML binding that reads that property updates automatically. This is the same pattern
you use with WPF or any MVVM binding, with QML items as the binding targets instead of XAML
controls.

See [C# and QML](csharp-and-qml.md) for a walkthrough of how a C# type, its properties, and its
methods appear on the QML side.

## Fine-tune what QML can see

If the default public-type surface is too broad, use
[`[Qt.Ignore]`](api/Qt.IgnoreAttribute.yml),
[`[Qt.IgnoreType]`](api/Qt.IgnoreTypeAttribute.yml), and
[`[Qt.Include]`](api/Qt.IncludeAttribute.yml) to adjust it.

Exclude a type defined in your project:

```csharp
[Qt.Ignore]
public class InternalHelper { }
```

Exclude a single property or method:

```csharp
public class UserProfile
{
    [Qt.Ignore]
    public string PasswordHash { get; set; }

    public string DisplayName { get; set; }
}
```

Exclude types by name at assembly level, including types from external assemblies you do not own:

```csharp
[assembly: Qt.IgnoreType(typeof(List<int>))]
[assembly: Qt.IgnoreType("Some.ThirdParty.InternalType")]
```

Exclude an entire type hierarchy and opt specific subtypes back in:

```csharp
[assembly: Qt.IgnoreType(typeof(BaseClass), Inherited = true)]

[Qt.Include]
public class SpecificSubclass : BaseClass { }
```

For the full attribute reference and all filtering patterns, see the
[API Reference](api-reference.md).

## Use existing collections as QML models

QML views such as [`ListView`](https://doc.qt.io/qt-6/qml-qtquick-listview.html) and
[`Repeater`](https://doc.qt.io/qt-6/qml-qtquick-repeater.html) need a model. You can use your
existing .NET collections directly:

* Plain arrays and [`List<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)
  work as static models.
* [`ObservableCollection<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1)
  works as a live model that tells the view about additions and removals.

Expose a collection as a property on any bridged type and bind the view to it in QML:

```csharp
public class AppState : INotifyPropertyChanged
{
    public ObservableCollection<string> Items { get; } = new();
    // ...
}
```

```qml
AppState {
    id: app
}

ListView {
    model: app.items
    delegate: Text { required property string item; text: item }
}
```

When you need control over paging, lazy loading, sorting, or hierarchical data, derive from
[`ListModel<T>`](api/Qt.Bridge.Models.ListModel-1.yml) or
[`TableModel<T>`](api/Qt.Bridge.Models.TableModel-1.yml) instead.
See [C# and QML](csharp-and-qml.md) for guidance on choosing between built-in collection support
and a custom model.

## Package application resources

Images, fonts, JSON files, and other assets that your QML interface needs must be packaged into
the Qt resource store. QML and C# both address them with `qrc:/` URLs at runtime.

Add a `<QtResource>` item for each file or glob you want to package:

```xml
<ItemGroup>
    <QtResource Include="images\logo.svg" />
    <QtResource Include="images\*" />
    <QtResource Include="fonts\Inter.ttf" />
    <QtResource Include="data\config.json" />
</ItemGroup>
```

The default URL uses your assembly resource ID and the file path from your project:

```text
qrc:/assemblies/<AssemblyId>/<relative-path>
```

For a project named `MyApp`, `images\logo.svg` becomes:

```text
qrc:/assemblies/MyApp/images/logo.svg
```

Reference that URL from QML wherever QML expects a URL:

```qml
Image {
    source: "qrc:/assemblies/MyApp/images/logo.svg"
}
```

Use [`Qt.Resources`](api/Qt.Resources.yml) when C# code needs to read the same packaged files:

```csharp
string json = Qt.Resources.ReadAllText("qrc:/assemblies/MyApp/data/config.json");
byte[] bytes = Qt.Resources.ReadAllBytes("qrc:/assemblies/MyApp/images/logo.svg");
```

For application icons, fonts, `.resx` discovery, access modes, alias overrides, referenced project
resources, and localization guidance, see [Application Resources](resources.md).

## Check your setup

After the first successful build, confirm that:

* The project builds without missing toolchain errors
* `dotnet run` launches the application and a window appears
* QML files are included in the project and visible at runtime
* Changes to exposed C# properties update the QML UI

## Common issues

If the build fails or the window does not appear, check these areas first:

* The .NET SDK version is older than .NET 8
* The C++ toolchain is not installed or not on `PATH`
* CMake or Ninja is missing
* On Linux, `QtDir` is not set or points to the wrong prefix
* The `.qml` file is outside the project directory and not picked up by the build
* The name passed to `LoadFromRootModule` does not match the `.qml` file name
* The first build has not completed yet; QML-facing C# types and editor support require a
  successful build
* A `qrc:/` URL in QML returns nothing — check that the file has a `<QtResource>` item and that
  the path matches `qrc:/assemblies/<AssemblyName>/<relative-path>`

## Where to go from here

* See [C# and QML](csharp-and-qml.md) for a deeper look at the mental model behind how C# and
  QML connect.
* See [Editing QML in Visual Studio](visual-studio-workflow.md) for QML diagnostics, completion,
  and semantic editor support after your first build.
* See [Application Resources](resources.md) for resource packaging, `qrc:/` URLs, and the
  [`Qt.Resources`](api/Qt.Resources.yml) API.
* See the [API Reference](api-reference.md) for the complete attribute, model base type, and
  startup API documentation.
