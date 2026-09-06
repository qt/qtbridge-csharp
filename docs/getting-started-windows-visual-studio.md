# Windows and Visual Studio

Use this workflow if you are on Windows and want the most integrated Qt Bridge for C# experience.
The Visual Studio extension adds project and item templates, and it can provide QML diagnostics,
completion, semantic editor support, and project-aware imports. Basic QML syntax highlighting and
completion are available when you open a QML file. The first build gives the editor the project
information it needs to understand QML-facing C# types and imports.

## Requirements

* Windows x64 or arm64
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
  <strong>Qt Bridge for C#</strong> or <strong>Qt</strong> to find the Qt Bridge for C#
  application template.
</p>

<p class="docs-hint">
  On Windows arm64, the package does not bundle Qt (unlike win-x64), so set the
  <code>QtDir</code> MSBuild property to a Qt 6 installation prefix before building, for example
  in the project's properties or a <code>Directory.Build.props</code> file.
</p>

For generated project structure, template options, and QML item templates, see
[Project Templates](templates-and-examples.md).

## Check your setup

After the first build, check that:

* The project builds without missing toolchain errors
* The application launches
* QML files are present and editable
* QML diagnostics and completion include QML-facing C# types after the first build

For the diagnostics, completion, semantic editor support, and project-aware imports the extension
provides, see [Editing QML in Visual Studio](visual-studio-workflow.md).

## Common first issues

If the project does not build or the editor support looks incomplete, check these areas first:

* The .NET SDK is missing or older than .NET 8
* Visual Studio is missing the C++ workload
* The first build has not completed yet; QML-facing C# types and editor support require a
  successful build
* The selected package or runtime does not match your Windows architecture (x64 or arm64)
