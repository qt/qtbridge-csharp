# Windows and Visual Studio

Use this workflow if you are on Windows and want the most integrated Qt Bridge for C# experience.
The Visual Studio extension adds project and item templates, and it can provide QML diagnostics,
completion, semantic editor support, and project-aware imports. Basic QML syntax highlighting and
completion are available when you open a QML file. The first build gives the editor the project
information it needs to understand QML-facing C# types and imports.

## Requirements

* Windows x64
* .NET SDK 8+
* Visual Studio 2022 or 2026
* .NET desktop development workload
* Desktop development with C++ workload
* CMake and Ninja available on `PATH`

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not
  need to write CMake files or C++ code for a template project.
</p>

## Create your first project

1. Install the Qt Bridge for C# Visual Studio extension.
2. Create a project from the Qt Bridge for C# project template.
3. Build the project once.
4. Open the QML files and inspect the editor support.
5. Run the application.

<p class="docs-hint">
  In the new project dialog, filter by <strong>C#</strong>, <strong>Windows</strong>, and
  <strong>Qt</strong> or <strong>QML</strong> to find the Qt Bridge QML application template.
</p>

## Check your setup

After the first build, check that:

* The project builds without missing toolchain errors
* The application launches
* QML files are present and editable
* QML diagnostics and completion include QML-facing C# types after the first build

## Common first issues

If the project does not build or the editor support looks incomplete, check these areas first:

* The .NET SDK is missing or older than .NET 8
* Visual Studio is missing the C++ workload
* The first build has not completed yet; QML-facing C# types and editor support require a
  successful build
* The selected package or runtime does not match Windows x64
