# Project Templates

Implementation notes for this page:

## Scope boundary

Owns:

* Project and item templates across .NET CLI, Visual Studio, and editor workflows.
* Template options such as `--Framework` and `--SampleCode`.
* The generated project shape and what each generated file is for.

Does not own:

* Platform-specific toolchain setup or first-build troubleshooting.
* Adding Qt Bridge for C# to an existing non-template project.
* Deep conceptual explanation of C# and QML data flow.

Current overlap note: some template-option details, such as `--Framework`, `--SampleCode`, and
the generated project shape, are currently mentioned in the platform getting-started pages. When
this page is written, those pages should keep only the shortest runnable workflow and link here for
template details.

## Content ideas

* Make this page the "next step" after the simple getting-started workflow.
* Explain what `dotnet new qt` creates.
* Explain what `dotnet new qml` creates.
* Explain that Visual Studio exposes equivalent project and item templates through the extension.
* Show the basic generated project shape:
  * Project file
  * C# entry point
  * Main QML file
* Show the minimal `Program.cs` entry point with `Qml.LoadFromRootModule(...)` and
  `Qml.WaitForExit(...)`.
* If examples are included, treat them as supporting material rather than the main page purpose.
* Map supporting examples to common learning goals:
  * Basic app startup
  * C# object exposed to QML
  * .NET collection or model used from QML
  * Resource usage
  * Custom model/data flow
* Keep this page action-oriented rather than conceptual.
* Add links to examples once the preferred example order is confirmed.
