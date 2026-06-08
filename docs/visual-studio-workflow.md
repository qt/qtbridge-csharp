# Editing QML in Visual Studio

Implementation notes for this page:

## Scope boundary

Owns:

* The Visual Studio QML editing experience after a Qt Bridge for C# project exists.
* Diagnostics, completion, semantic editor support, project-aware imports, and troubleshooting editor state.
* Explaining why the first build affects bridge-aware completion and diagnostics for generated
QML-facing C# types.

Does not own:

* Installing Visual Studio workloads or creating the first Visual Studio project.
* Full project-template documentation.
* Package selection for CLI or existing-project workflows.

## Content ideas

* Visual Studio workflow page around QML editing support.
* Avoid repeating the full Windows and Visual Studio setup guide.
* Explain that the extension helps Visual Studio understand QML files in Qt Bridge for C# projects.
* Cover QML diagnostics.
* Cover completion.
* Cover semantic editor support.
* Cover project-aware imports.
* Explain that basic QML syntax highlighting and completion are available immediately, while
  bridge-aware support for generated C# types depends on a successful build.
* Mention that the extension recognizes Qt Bridge for C# projects.
* Mention that templates are covered on the Project Templates page.
* Include a small "when something looks missing" checklist later:

  * Build the project once.
  * Confirm the QML file belongs to a Qt Bridge for C# project.
  * Check the Qt Bridge output pane.
* Keep this page editor-focused, not package/setup-focused.

