# VS Code on Windows, Linux, and macOS

Use this workflow if you work in VS Code. The Qt Extension for VS Code creates Qt Bridge for C#
projects, helps you select the Qt installation for builds, and provides QML editing, QML Preview,
QML Profiler, and QML debugging. Basic QML syntax highlighting and completion are available when
you open a QML file. The first build gives the QML tools the project information they need to
understand QML-facing C# types and imports.

<p class="docs-hint">
  The Qt Bridge for C# support in VS Code is a preview, like Qt Bridge for C# itself. For more
  details, see the
  <a href="https://doc-snapshots.qt.io/vscodeext-dev/vscodeext-tutorials-qt-csharp.html">Qt
  Extension for VS Code documentation</a>.
</p>

## Requirements

* Windows x64 or arm64, Linux x64 or arm64, or macOS arm64
* .NET SDK 8+
* CMake and Ninja available on `PATH`
* A C++ toolchain
* A Qt 6 installation, except on Windows x64, where the package bundles Qt
* VS Code with the **Qt Bridge C#** extension. It needs the **Qt Core** extension, and the
  **Qt QML** extension provides the QML tools. The **Qt Extension Pack** installs all of them.
* Optionally, the **C# Dev Kit** extension for editing and debugging C# code

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not
  need to write CMake files or C++ code for a template project.
</p>

## Create your first project

The Qt extension includes the Qt Bridge for C# templates, so you do not need to install them with
`dotnet new install`:

1. Run **Qt: Create a new project or file** from the Command Palette, or select **New project** on
   the Qt **Welcome** page.
2. Select **Qt Bridge for C# application**.
3. Enter the name and location, select the target framework, and optionally add the sample code.
4. Open the project folder in VS Code.

To add a QML file to a project, run the same command and select **Qt Bridge for C# QML file**.

The generated project matches what `dotnet new qt` creates. See
[Project Templates](templates-and-examples.md) for the project structure.

## Select the Qt installation

Building a Qt Bridge for C# project needs a Qt installation, which the build reads from `QtDir`.
Only the Windows x64 package bundles Qt. On Windows arm64, Linux, and macOS, `QtDir` is required.
The selected Qt prefix must contain `lib/cmake/Qt6/Qt6Config.cmake`.

When you open a project whose build would not find Qt, the extension asks you to select one. To
select it later, run **Qt Bridge C#: Select Qt Installation**. The command lists the Qt
installations registered in the Qt extension that can build on your machine, or lets you browse
for one. It stores the selection in the `qt-csharp.qtDir` setting.

The extension sets that Qt as `QtDir` in new terminals in the **Terminal** view, so `dotnet build`
and `dotnet run` there use the same Qt as the QML tools. Builds that start outside the terminal,
for example from C# Dev Kit, do not see the setting. For them, set `QtDir` as an environment
variable before you start VS Code, or set the `QtDir` property in the project.

## Build and run

Build and run the project in the **Terminal** view:

```bash
dotnet build
dotnet run
```

With C# Dev Kit, you can also build, run, and debug the application from the **Solution Explorer**
view: right-click the project, and select **Build** or **Debug**.

The first build also generates the metadata that the QML tools need, so build the project before
you use them.

## Check your setup

After the first build, check that:

* The project builds without missing Qt or toolchain errors
* The application launches
* QML diagnostics and completion include QML-facing C# types
* **Qt: Preview current QML file live** shows the QML file that is open in the editor

For QML editing, QML Preview, QML Profiler, and QML debugging, see
[Working in VS Code](vscode-workflow.md).

## Common first issues

If the project does not build or the QML tools do not work, check these areas first:

* The .NET SDK is missing or older than .NET 8
* CMake, Ninja, or the C++ toolchain is missing
* No Qt installation is selected, or the build runs outside the **Terminal** view and does not
  see `QtDir`
* The first build has not completed yet; QML-facing C# types and QML Preview require a successful
  build
