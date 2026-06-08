# Adding QML to Existing C# Projects

Implementation notes for this page:

## Scope boundary

Owns:

* The migration workflow for adding Qt Bridge for C# to an existing C# application.
* Package selection, startup shape, and how existing objects become part of the QML-facing app model.
* Decisions that teams face after they already have a C# codebase and want to introduce QML.

Does not own:

* The shortest first-project setup workflow for new users.
* Full project-template documentation or generated project structure.
* Conceptual explanations of QML syntax beyond what is needed for migration.

Current overlap note: the platform getting-started pages currently include short "Add Qt Bridges -
C# to an existing project" sections. When this page is written, those sections should stay minimal
and link here for migration details, package choice, startup shape, and exposing existing code to
QML.

## Content ideas

* Make this page the main page for the advanced use case.
* Audience: C# developers who already tried the bridge and want to add QML support to an existing C# codebase.
* Cover the Visual Studio workflow:
  * Use NuGet Package Manager.
  * Install the Qt Bridge for C# package in the app project.
  * Add QML files to the project.
* Cover the CLI/editor workflow:
  * Use `dotnet add package`.
  * Choose the package that matches the target runtime.
  * Build from the command line or editor terminal.
* Explain the app startup shape:
  * Call `Qml.Load(...)` from `Main()`.
  * Call `Qml.WaitForExit(...)` from `Main()`.
* Explain how existing C# objects become part of the QML-facing app model.
* Explain how to fine-tune which C# types are exposed to QML.
* Point to attributes such as include/ignore/type filtering once examples are ready.
* Explain how existing .NET collections can be used from QML views.
* Mention when custom data models are useful.
* Keep build-from-source and contributor details out of this page.
