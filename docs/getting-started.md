# Getting Started

Choose the workflow that matches your operating system and development environment. If you are new
to Qt/QML, start with a template project, build it once, and inspect how the C# code and QML files
relate to each other.

For an integrated experience, use VS Code on Windows, Linux, and macOS, or Visual Studio on Windows.
Use the CLI workflow if you prefer another editor or a terminal-first workflow.

## Choose your workflow

### [VS Code on Windows, Linux, and macOS](getting-started-vscode.md)

Use this workflow if you work in VS Code. The Qt Extension for VS Code creates projects, helps you
select the Qt installation, and provides QML editing, QML Preview, and QML debugging.

### [Windows and Visual Studio](getting-started-windows-visual-studio.md)

Use this workflow if you work in Visual Studio on Windows. The extension provides project and item
templates, plus bridge-aware QML editor support after the first build.

### [Windows with .NET CLI](getting-started-windows-cli-vscode.md)

Use this workflow if you prefer command-line project creation or want to inspect the template
output directly.

### [Linux with .NET CLI](getting-started-linux.md)

Use this workflow if you are evaluating Qt Bridge for C# on Linux or WSL. Linux requires an external
Qt installation selected during build.

### macOS with .NET CLI

macOS arm64 (Apple Silicon) is also supported, following the same CLI workflow as Linux: install
the templates, then build against an external Qt installation selected with `QtDir`. Use the
`QtGroup.Qt.Bridge.CSharp.osx-arm64` package — see
[Adding QML to Existing C# Projects](existing-csharp-projects.md) for package selection details.

## Common requirements

All workflows require:

* .NET SDK 8+
* CMake and Ninja
* A C++ toolchain

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not need
  to write CMake files or C++ code for a template project.
</p>

Windows workflows target Windows x64 or arm64. Linux workflows target Linux x64 or arm64, and macOS
workflows target macOS arm64; both require a Qt 6 installation that contains
`lib/cmake/Qt6/Qt6Config.cmake`.

## What the first build does

The first successful build is important because the bridge setup is more than a plain C# compile.
It:

* Restores the bridge package
* Runs the native and managed build steps needed by the bridge
* Prepares the bridge information and native support used by the QML side
* Makes exposed C# types available to QML
* Enables bridge-aware QML editor support in VS Code and Visual Studio

Basic QML syntax highlighting and completion are available when you open a QML file. The first
build gives the editor the project information it needs to understand QML-facing C# types and
imports.

## Where to go from here

* See [C# and QML](csharp-and-qml.md) for the mental model behind how your C# code and QML
  interface connect.
* See [Project Templates](templates-and-examples.md) for the generated project structure and
  template options.
* See [Adding QML to Existing C# Projects](existing-csharp-projects.md) if you want to add the
  bridge to an app you already have.
