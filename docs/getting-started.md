# Getting Started with Qt Bridges C#

## Prerequisites

- **.NET adapter** to host the Qt/QML engine from managed code.
- **Source/code generator** that discovers your C# types and exposes them to QML.
- **Filtering rules** (`[Qt.Include]`, `[Qt.Ignore]`, `[Qt.IgnoreType]`) to control what becomes visible to QML.
- **C++ include headers** used by the native side of the bridge.
- A **minimal, open-source Qt Quick runtime subset** sufficient to load and run QML (no full or commercial Qt installation).

## Getting started

To get started, see the [Getting Started with Qt Quick C#](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-getting-started.html) tutorial.

## How it works

1. Reference the package in your .NET 8 project.
2. Mark types/members you want to expose to QML using attributes:
   - `[Qt.Include]` - opt-in a type or a specific member.
   - `[Qt.Ignore]` - remove a type or member.
   - `[assembly: Qt.IgnoreType(...)]` - remove one or more types, or entire type hierarchies by rule; supports `Inherited = true`.
3. Add your QML (e.g., `Main.qml`) to the project.
4. Build & run. The generator creates the interop surface; the adapter boots the QML engine and loads your entry file.

## Add Qt Quick C# to your project

To start using the Qt Quick C# in your project, add the `Qt.Bridge.CSharp` package to your project:

```shell
# Run in the project folder
dotnet add package QtGroup.Qt.Bridge.CSharp.win-x64 --version 0.1.0.1-alpha
```

### Select the types of your data Model to expose to QML Views

By default, all public types in the project are exposed to QML.

```csharp
// This class will be included
public class Foo
{
    // This property will be included
    public int Bar { get; set; }
}
```

To control what gets exposed or hidden, mark types/members with attributes:

- `[Qt.Include]` - expose a type or a specific member.
- `[Qt.Ignore]` - remove a type or member.
- `[assembly: Qt.IgnoreType(...)]` - remove one or more types, or entire type hierarchies by rule; supports `Inherited = true`.

For example:

```csharp
[assembly: Qt.IgnoreType(typeof(Foo), Inherited = true)]

// This class will be ignored (ignored base type)
public class Foo
{ }

// This class will be ignored (derived type of ignored base type)
public class Oof : Foo
{ }

// This class will be included (opted-in by attribute)
[Qt.Include]
public class Bar : Foo
{ }
```

### Add a QML View to your project

Create a QML file (e.g., `Main.qml`) that uses your C# types.

For example, given the following C# type:

```csharp
using Qt.Bridge.Utils;

namespace MyProject
{
    public class MyClass
    {
        public int MyProperty { get; set; }
    }
}
```

To access C# types from QML, instantiate them in QML and access their members via a `.` operator. The member names match the C# names, but are started in lowercase:

```qml
import QtQuick
// ...
Rectangle {
    width: 400; height: 400

    MyClass {
        id: myClassInstance
    }

    Text {
        text: "MyProperty value is " + myClassInstance.myProperty
        anchors.centerIn: parent
    }
}
```

### Load the QML View from C# code

In your C# code, load the QML file using `Qml.LoadFromRootModule`:

```csharp
using Qt.Quick;

namespace MyProject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("Main");
            Qml.WaitForExit();
        }
    }
}
```

Build and run. The generator creates the interop surface; the adapter boots the QML engine and loads your entry file.

## Tutorial

This tutorial shows how to create, run, and modify a Qt Quick C# application: [Getting Started with Qt Quick C#](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-getting-started.html).

## How-to guides

### Using project and item templates

See [Using project and item templates](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-project-templates.html) for instructions on using the provided project and item templates to create Qt Quick C# applications.

### Selecting types to expose to QML Views

See [Using C# Models with QML Views](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-models.html) for instructions on selecting the types to expose to QML Views.

### Debugging generated code

See [Debugging generated code](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-debugging.html) for instructions on debugging the generated native code.

### Building from source

See [Building from source](https://doc-snapshots.qt.io/qtbridges-dev/qtbridges-csharp-build-from-source.html) for instructions on building the Qt Bridges package from source.

