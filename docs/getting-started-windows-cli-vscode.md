# Windows with .NET CLI

Use this workflow if you prefer command-line project creation or want to inspect the template
output directly. The same `dotnet` commands work from a terminal, from the integrated terminal of
an editor, or from any editor that opens the generated project folder.

<p class="docs-hint">
  If you work in VS Code, the Qt Extension for VS Code creates projects, selects the Qt installation,
  and provides QML editing, preview, and debugging. See
  <a href="getting-started-vscode.md">VS Code on Windows, Linux, and macOS</a>.
</p>

## Requirements

* Windows x64 or arm64
* .NET SDK 8+
* CMake and Ninja available on `PATH`
* A C++ toolchain

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not
  need to write CMake files or C++ code for a template project. On Windows, the C++ toolchain usually
  comes from Visual Studio or the Visual Studio Build Tools C++ workload.
</p>

## Create a project from templates

Install the templates:

```bash
dotnet new install QtGroup.Qt.Bridge.CSharp.Templates
```

Create, build, and run a project:

```bash
dotnet new qt -n MyQtApp
cd MyQtApp
dotnet build
dotnet run
```

For template options such as `--Framework` and `--SampleCode`, generated project structure, and QML
item templates, see [Project Templates](templates-and-examples.md).

<p class="docs-hint">
  On Windows arm64, the package does not bundle Qt (unlike win-x64), so point <code>QtDir</code>
  at a Qt 6 installation: <code>dotnet build -p:QtDir=D:\Qt\6.11.0\msvc2022_arm64</code>.
</p>

## Add Qt Bridge for C# to an existing project

For an existing Windows project, add the package matching your architecture:

```bash
# Windows x64
dotnet add package QtGroup.Qt.Bridge.CSharp.win-x64 --version 0.4.*-*

# Windows arm64
dotnet add package QtGroup.Qt.Bridge.CSharp.win-arm64 --version 0.4.*-*
```

The package is platform-specific because the bridge includes native runtime pieces for the target
platform. The `0.4.*-*` version range stays on the 0.4 pre-release train.

Then build and run from the command line or your editor:

```bash
dotnet build
dotnet run
```

On Windows arm64, the package does not bundle Qt, so point `QtDir` at a Qt 6 installation instead:

```bash
dotnet build -p:QtDir=D:\Qt\6.11.0\msvc2022_arm64
dotnet run -p:QtDir=D:\Qt\6.11.0\msvc2022_arm64
```

For the full migration workflow, including QML files, startup calls, and exposing your existing
C# objects to QML, see [Adding QML to Existing C# Projects](existing-csharp-projects.md).

## Check your setup

After the first build, check that:

* The project builds without missing toolchain errors
* The application launches
* QML files are present in the project folder
* The project contains the expected C# entry point and `Main.qml`

## Common first issues

If the project does not build or run, check these areas first:

* The .NET SDK is missing or older than .NET 8
* The C++ toolchain is not installed or not available to the build
* The selected package or runtime does not match your Windows architecture (x64 or arm64)
* The terminal is not running from the project directory
