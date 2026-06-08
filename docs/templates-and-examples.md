# Project Templates

Project templates give you a working C# and QML application without wiring the bridge by hand. This
page explains what the templates create, which options are available, and how to add more QML files
as the project grows.

## Create a project

The .NET CLI, Visual Studio, and editor workflows all use the same underlying templates, so the
generated project looks the same no matter which one you choose.

### From the .NET CLI

Install the templates once, then create a project:

```bash
dotnet new install QtGroup.Qt.Bridge.CSharp.Templates
dotnet new qt -n MyQtApp
```

`dotnet new qt` creates a new C# and QML application project. Two options shape what it generates:

* `--Framework` selects the target framework, for example `net9.0`. The default is `net8.0`.
* `--SampleCode` adds a small counter sample that wires a C# object to a QML button and label, so
  you have something running and bound end to end before you write your own code.

```bash
dotnet new qt -n MyQtApp --Framework net9.0 --SampleCode
```

To add another QML file to an existing Qt Bridge for C# project, use the item template:

```bash
dotnet new qml --FileName=MainPage
```

### From Visual Studio

The Visual Studio extension exposes the same templates as project and item templates in the
**Create a new project** and **Add new item** dialogs. Filter by **C#**, **Windows**, and **Qt** or
**QML** to find the Qt Bridge for C# QML application template.

Use **Add &gt; New Item** in a Qt Bridge for C# project to add a QML file. The generated project
shape, framework selection, and sample-code option match what you get from `dotnet new qt`.

## What the project template creates

A project created without `--SampleCode` has three files at its core:

```text
MyQtApp/
  MyQtApp.csproj
  Program.cs
  Main.qml
```

* **`MyQtApp.csproj`** is a regular .NET project file. It references the Qt Bridge for C# package
  for your platform and includes `Program.cs` and `Main.qml` as project items. You build and run it
  the same way you would any other .NET project, with `dotnet build` and `dotnet run` or from your
  editor.
* **`Program.cs`** is the C# entry point. It loads the QML side and keeps the application running
  until the QML window closes.
* **`Main.qml`** is the first QML file the application loads. It describes the window and the user
  interface that appears when the application starts.

`--SampleCode` adds a small `Counter` C# class that QML can see, and extends `Main.qml` with a
button and a label bound to it. Use it as a tour of how a C# object and a QML item connect; remove
it once you start building your own UI.

## The minimal entry point

Every template project starts QML the same way:

```csharp
public class Program
{
    internal static void Main(string[] args)
    {
        Qml.LoadFromRootModule("Main");
        Qml.WaitForExit();
    }
}
```

* [`Qml.LoadFromRootModule("Main")`](api/Qt.Quick.Qml.yml) loads `Main.qml` as the application's
  root QML file and creates the window it describes.
* [`Qml.WaitForExit()`](api/Qt.Quick.Qml.yml) blocks `Main` until the QML side shuts down, so the
  process stays alive while the UI runs.

You do not need to change either call for a typical project. Add your own C# types above `Program`,
expose them to QML the way [C# and QML](csharp-and-qml.md) describes, and reference them from
`Main.qml` or from QML files you add with the item template.

## Examples

The repository includes a separate
[examples overview](https://github.com/qt/qtbridge-csharp/tree/dev/examples) with runnable sample
applications.

Use the examples after you understand the generated project shape. They go beyond the template and
show common next steps, including C# objects exposed to QML, model/view data, resources, and larger
multi-file UI flows.
