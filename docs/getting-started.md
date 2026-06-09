# Getting Started

Choose the workflow that matches your operating system and development environment. If you are new
to Qt/QML, start with a template project, build it once, and inspect how the C# code and QML files
relate to each other.

Use the Visual Studio workflow for the most integrated experience on Windows. Use the CLI workflow
if you prefer Visual Studio Code, another editor, or a terminal-first workflow.

## Choose your workflow

### [Windows and Visual Studio](getting-started-windows-visual-studio.md)

Use this workflow if you work in Visual Studio on Windows and want the most integrated experience.
The extension provides project and item templates, plus bridge-aware QML editor support after the
first build.

### [Windows with .NET CLI or VS Code](getting-started-windows-cli-vscode.md)

Use this workflow if you prefer command-line project creation, work in Visual Studio Code, or want
to inspect the template output directly.

### [Linux with .NET CLI](getting-started-linux.md)

Use this workflow if you are evaluating Qt Bridge for C# on Linux or WSL. Linux requires an external
Qt installation selected during build.

## Common requirements

All workflows require:

* .NET SDK 8+
* CMake and Ninja
* A C++ toolchain

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not need
  to write CMake files or C++ code for a template project.
</p>

Windows workflows target Windows x64. Linux workflows target Linux x64 and require a Qt 6
installation that contains `lib/cmake/Qt6/Qt6Config.cmake`.

## What the first build does

The first successful build is important because the bridge setup is more than a plain C# compile.
It:

* Restores the bridge package
* Runs the native and managed build steps needed by the bridge
* Prepares the bridge information and native support used by the QML side
* Makes exposed C# types available to QML
* Enables bridge-aware QML editor support when using Visual Studio

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
