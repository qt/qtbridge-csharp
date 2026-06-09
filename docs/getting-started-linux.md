# Linux with .NET CLI

Use this workflow if you are evaluating Qt Bridge for C# on Linux or WSL. Linux does not bundle Qt
with the bridge package, so the build must be pointed at a compatible Qt 6 installation.

## Requirements

* Linux x64
* .NET SDK 8+
* CMake and Ninja available on `PATH`
* A C++ toolchain such as `build-essential`
* A compatible Qt 6 installation

<p class="docs-hint">
  CMake, Ninja, and the C++ toolchain are used by the bridge build behind the scenes; you do not
  need to write CMake files or C++ code for a template project.
</p>

The selected Qt prefix must contain `lib/cmake/Qt6/Qt6Config.cmake`. On Linux, install Qt 6 from
your distribution packages or the Qt online installer, then point `QtDir` at that Qt prefix.

## Create a project from templates

Install the templates:

```bash
dotnet new install QtGroup.Qt.Bridge.CSharp.Templates
```

Create a project:

```bash
dotnet new qt -n MyQtApp
cd MyQtApp
```

Build with `QtDir` pointing to your Qt installation:

```bash
dotnet build -p:QtDir=/path/to/qt-prefix
dotnet run
```

For template options such as `--Framework` and `--SampleCode`, generated project structure, and QML
item templates, see [Project Templates](templates-and-examples.md).

## Add Qt Bridge for C# to an existing project

For an existing Linux x64 project, add the Linux package:

```bash
dotnet add package QtGroup.Qt.Bridge.CSharp.linux-x64 --version 0.3.*-*
```

The package is platform-specific because the bridge includes native build/runtime pieces for the
target platform. The `0.3.*-*` version range stays on the 0.3 pre-release train.

Then build with `QtDir`:

```bash
dotnet build -p:QtDir=/path/to/qt-prefix
```

For the full migration workflow, including QML files, startup calls, and exposing your existing
C# objects to QML, see [Adding QML to Existing C# Projects](existing-csharp-projects.md).

## Check your setup

After the first build, check that:

* The project builds without missing Qt or toolchain errors
* The application launches
* QML files are present in the project folder
* `QtDir` points to a Qt prefix containing `lib/cmake/Qt6/Qt6Config.cmake`

## Common first issues

If the project does not build or run, check these areas first:

* The .NET SDK is missing or older than .NET 8
* Required Linux build packages are missing
* `QtDir` points to the wrong directory
* Qt cannot be found by CMake
* The selected package or runtime does not match Linux x64
