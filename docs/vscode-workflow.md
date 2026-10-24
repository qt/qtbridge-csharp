# Working in VS Code

The Qt Extension for VS Code supports Qt Bridge for C# projects on Windows, Linux, and macOS. It
provides the QML language server, QML Preview, QML Profiler, and QML debugging for your QML code.
This page covers these tools and what to check when something does not work.

To install the extensions, create a project, and select the Qt installation, see
[VS Code on Windows, Linux, and macOS](getting-started-vscode.md).

<p class="docs-hint">
  The Qt Bridge for C# support in VS Code is a preview, like Qt Bridge for C# itself. For more
  details, see the
  <a href="https://doc-snapshots.qt.io/vscodeext-dev/vscodeext-tutorials-qt-csharp.html">Qt
  Extension for VS Code documentation</a>.
</p>

## Edit QML

The Qt QML extension runs the QML language server for QML files in Qt Bridge for C# projects.
Basic QML support is available right away. After the first build, completion and diagnostics also
cover the types that you expose from C#, and the language server picks up later builds
automatically.

If you build the project in several configurations, such as `Debug` and `Release`, or for several
target frameworks, the extension asks which build the QML tools should use. To change the
selection later, run **Qt Bridge C#: Select QML Build Metadata**.

## Preview QML

To preview the QML file in the editor, select **Qt: Preview current QML file live** in the Command
Palette or on the editor toolbar. The extension starts your application, and changes to any QML
file of the project appear in the preview when you save the file.

The preview always starts with the saved QML code, so you do not need to build after changing QML
code. Changes to the C# code and new QML files take effect after you build the project again.

To profile the QML code, run **Qt: Profile QML application**.

## Debug

Projects that you create with the **Qt Bridge for C# application** template have a configuration
for the QML debugger in `.vscode/launch.json`. To debug the QML code, select **Qt Bridge: QML** in
the **Run and Debug** view, and press **F5**.

For other projects, add the configuration to `launch.json`. It uses
`${command:qt-csharp.launchTargetPath}` as the program:

```json
{
  "name": "Qt Bridge: QML",
  "type": "qml",
  "request": "launch",
  "program": "${command:qt-csharp.launchTargetPath}"
}
```

The command resolves the executable of the project that contains the file in the editor. It does
not build the project, so build it first. Breakpoints in QML code that runs right when the
application starts can be missed.

To debug the C# code, use the C# debugger of C# Dev Kit: right-click the project in the
**Solution Explorer** view, and select **Debug**.

## When something does not work

* **The build cannot find Qt.** Run **Qt Bridge C#: Select Qt Installation**. For builds outside
  the **Terminal** view, set `QtDir` in the environment or in the project. See
  [Select the Qt installation](getting-started-vscode.md#select-the-qt-installation).
* **A build in the Terminal view still uses the previous Qt.** Terminals that were open when you
  selected a different Qt keep the previous `QtDir`. Relaunch them, or open new ones. See
  [Select the Qt installation](getting-started-vscode.md#select-the-qt-installation).
* **QML Preview asks you to build the project.** Build it once. If you built it in several
  configurations, select the build with **Qt Bridge C#: Select QML Build Metadata**.
* **Check the output.** The extensions log to the `qt-csharp` and `qt-qml` channels in the
  **Output** view. Include these logs in issue reports.

## Where to go from here

* See [C# and QML](csharp-and-qml.md) for how C# types, properties, and methods appear on the
  QML side.
* See [Project Templates](templates-and-examples.md) for the generated project structure.
* See the [Qt Extension for VS Code documentation](https://doc-snapshots.qt.io/vscodeext-dev/index.html)
  for the QML tools in general.
