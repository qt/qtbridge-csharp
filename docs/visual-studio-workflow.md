# Editing QML in Visual Studio

The Qt Bridge for C# Visual Studio extension recognizes Qt Bridge for C# projects and starts a
QML language server for `.qml` files in those projects. This page covers the editing experience
the extension provides: diagnostics, completion, semantic highlighting, and project-aware
imports, plus what to check when that support does not appear.

For installing Visual Studio workloads and creating your first project, see
[Windows and Visual Studio](getting-started-windows-visual-studio.md). For template options and
project structure, see [Project Templates](templates-and-examples.md).

## What works before the first build

Basic QML syntax highlighting and completion for QML and Qt Quick types are available as soon as
you open a `.qml` file in a Qt Bridge for C# project — no build required.

Bridge-aware support depends on a successful build. Diagnostics and completion for your
QML-facing C# types — the properties, methods, and models you expose from C# — become available
once the project has built at least once. The first build produces the metadata the extension
uses to configure the language server for your project, including the import paths and bridge
information for your C# types.

If you open a `.qml` file before the project has built, the extension shows a dismissible
notification bar reminding you to build the project for full QML support. The notification
appears once per project per Visual Studio session.

## Diagnostics

The language server reports QML diagnostics inline as you edit: syntax errors, unresolved types,
and unresolved imports. Before the first build, diagnostics for your own C# types appear as
unresolved because the bridge information for those types is not available yet. After a
successful build, those types resolve and the related diagnostics clear.

## Completion

Completion covers QML and Qt Quick elements, properties, and signal handlers from the start.
After the first build, completion also includes the properties and methods your C# types expose
to QML, using the same `camelCase` names QML sees at runtime. See
[C# and QML](csharp-and-qml.md) for how C# members map to QML names.

## Semantic editor support

Semantic highlighting classifies QML built-in types, your own QML-facing C# types, properties,
and signal handlers so Visual Studio can style them distinctly. When a build finishes and the
extension picks up the updated metadata, it refreshes semantic highlighting for open documents so
newly resolved types are classified correctly without closing and reopening the file.

## Project-aware imports

The extension resolves QML import paths for your project automatically, including:

* Built-in Qt modules such as `QtQuick` and `QtQuick.Controls`.
* The QML module information produced by your project's build, so `import` statements that
  reference your own QML-facing C# types resolve correctly.

These import paths come from the Qt Bridge for C# package and from your project's build output,
so you do not need to configure import paths yourself.

## Logging for issue reports

The extension can write two optional log files: a QML Language Server process log and an LSP traffic
log between Visual Studio and the language server. Both are off by default. Open **Extensions > Qt
Bridge for C# > Options...** and enable logging under **Qt Bridge for C# > Logging**, choosing a log
directory for each. Include these logs when reporting QML editing issues or investigating language
server behavior.

## When something looks missing

If diagnostics, completion, or highlighting for your own C# types are missing or stale, check the
following:

* **Build the project once.** Bridge-aware support requires a successful build to produce the
  metadata the language server uses.
* **Confirm the QML file belongs to a Qt Bridge for C# project.** The extension only activates
  for `.qml` files inside a project that references the Qt Bridge for C# package.
* **Check the Qt Bridge for C# output pane.** The extension logs language server startup,
  metadata updates, and errors there, including any issues installing or launching the language
  server.

## Where to go from here

* See [C# and QML](csharp-and-qml.md) for how C# types, properties, and methods appear on the
  QML side.
* See [Adding QML to Existing C# Projects](existing-csharp-projects.md) for adding the bridge to
  an existing project.
* See [Project Templates](templates-and-examples.md) for creating a new project.
