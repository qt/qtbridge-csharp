# Visual Content Plan

Internal planning notes for adding diagrams, screenshots, and code examples to the documentation.

## Scope boundary

Owns:

* Visual planning guidance for diagrams, screenshots, and snippets across documentation pages.
* Decisions about where visuals help orientation, confidence, or review discussions.
* Stable visual direction that can be referenced while individual pages are being written.

Does not own:

* Final prose for the content pages.
* API documentation content or generated API examples.
* Page-specific setup instructions beyond visual recommendations.

## General guidance

* Use visuals for orientation and confidence, not decoration.
* Prefer stable diagrams and code snippets when screenshots would age quickly.
* Use one screenshot per UI moment; do not maintain separate light and dark screenshots unless
  there is a clear reason.
* Frame screenshots in a neutral bordered container so they work in both light and dark docs.
* Keep diagrams theme-aware where possible.
* Avoid generic marketing imagery.
* Avoid command-line screenshots; use code blocks instead.

## Page-by-page plan

### Landing page

* Keep clean.
* No image.
* Purpose: onboarding and routing.

### Introduction

* Add a simple architecture diagram.
* Suggested shape:
  * C# application logic, models, and resources
  * Qt Bridge for C#
  * QML
  * Qt Quick UI
* Maybe prefer SVG or HTML/CSS over screenshot.
* Maybe make it theme-aware.

### Getting Started

* Add one screenshot of the running template app so users know what `dotnet run` should open.
* Keep it as a chooser page.

### Windows and Visual Studio

* Add one screenshot.
* Show the Visual Studio "Create a new project" dialog with the Qt Bridge template visible.
* Use one consistent IDE theme.
* Prefer dark theme unless someone requests light theme for readability.

### Windows with .NET CLI or VS Code

* No screenshot.
* Use code blocks.

### Linux with .NET CLI

* No screenshot.
* Use code blocks.

### C# and QML

* Add a short side-by-side example.
* Possible shape:
  * C# object or model on one side
  * QML binding/use on the other side
* Keep the example small and conceptual.
* Use this page to show that normal C# code can drive a QML UI.

### Adding QML to Existing C# Projects

* Add a before/after project structure diagram.
* Posible shape:
  * Before: existing C# application
  * After: package reference, QML file, startup calls such as `Qml.Load(...)` and `Qml.WaitForExit(...)`
* Use a screenshot only if showing the NuGet Package Manager flow is important.

### Editing QML in Visual Studio

* Add one screenshot.
* Show QML completion or diagnostics in Visual Studio.
* Purpose: demonstrate the extension value visually.

### Project Templates

* Consider a minimal Hello World tutorial.
* Show the generated project tree.
* Include small `Program.cs` and `Main.qml` snippets.
* If the running-app screenshot is not shown on Getting Started, show it here near the template walkthrough.
* Link to deeper examples.
* Screenshot is optional; snippets and project tree may be more durable.

## Screenshot theme choice

* Use one screenshot theme consistently.
* Preferred default: dark theme for IDE/editor screenshots.
* Do not create separate light and dark screenshot sets unless there is a strong reason.
